using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Brouj.Application.Common.Settings;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.Interceptors;
using Brouj.Infrastructure.Persistence;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace Brouj.Infrastructure.IntegrationTests.Performance;

/// <summary>Owns only generated resources. Never reads developer secrets or an application database.</summary>
internal sealed class BaselineEnvironment(ILogger<BaselineEnvironment> logger) : IAsyncDisposable
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Brouj-" + Guid.NewGuid().ToString("N") + "aA1!").Build();
    private readonly JwtOptions _jwt = new()
    {
        Issuer = "brouj-local-baseline",
        Audience = "brouj-local-generator",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    };
    private readonly string _certificatePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private Process? _api;
    private Task? _stdout;
    private Task? _stderr;
    private byte[]? _certificateHash;
    private string _connectionString = "";
    private string _directory = "";
    private string _origin = "";

    public List<(Guid Unit, Guid Project)> Units { get; } = [];
    public Guid[] Customers { get; private set; } = [];
    public Guid Admin { get; private set; }
    public List<BaselineSession> Sessions { get; } = [];
    public BaselineSession Staff { get; private set; } = null!;
    public string ContainerId => _container.Id;

    public async Task StartAsync(string root, string output, CancellationToken cancellationToken,
        bool applyMigrations = false)
    {
        _directory = Path.Combine(output, "private");
        Directory.CreateDirectory(_directory);
        logger.LogInformation("Starting isolated SQL Server and seeding synthetic baseline data");
        await _container.StartAsync(cancellationToken);
        _connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "BroujPart11_" + Guid.NewGuid().ToString("N"),
            MaxPoolSize = 100,
            ApplicationName = "Brouj.Part11"
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_connectionString).Options;
        await using var context = new ApplicationDbContext(options,
            new AuditableEntityInterceptor(TimeProvider.System),
            new ActivityLogInterceptor(new TestActor(), TimeProvider.System));

        // Final release HTTP checks also reuse this isolated real-process fixture on a migrated database.
        // Preserve the original performance baseline's schema-creation path unless explicitly requested.
        if (applyMigrations)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        await SeedAsync(cancellationToken);

        // The listener binds only loopback. Pin the generated certificate instead of accepting arbitrary TLS peers.
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        _origin = $"https://localhost:{port}";
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        _certificateHash = certificate.GetCertHash(HashAlgorithmName.SHA256);
        var certificatePath = Path.Combine(_directory, "localhost.pfx");
        await File.WriteAllBytesAsync(certificatePath,
            certificate.Export(X509ContentType.Pfx, _certificatePassword), cancellationToken);

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.Combine(root, "src", "Brouj.API"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(Path.Combine(root, "src", "Brouj.API", "bin", "Release", "net10.0", "Brouj.API.dll"));

        // Required traffic budgets are synthetic baseline settings, never production defaults.
        var configuration = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["DOTNET_ENVIRONMENT"] = "Production",
            ["ASPNETCORE_URLS"] = _origin,
            ["ASPNETCORE_Kestrel__Certificates__Default__Path"] = certificatePath,
            ["ASPNETCORE_Kestrel__Certificates__Default__Password"] = _certificatePassword,
            ["ConnectionStrings__DefaultConnection"] = _connectionString,
            ["InitialAdmin__Enabled"] = "false",
            ["Jwt__Issuer"] = _jwt.Issuer,
            ["Jwt__Audience"] = _jwt.Audience,
            ["Jwt__SigningKey"] = _jwt.SigningKey,
            ["Caching__Provider"] = "Memory",
            ["FileStorage__RootPath"] = Path.Combine(_directory, "uploads"),
            ["AllowedHosts"] = "localhost",
            ["Api__BaseUrl"] = _origin,
            ["Api__AllowedOrigins__0"] = "https://brouj.com",
            ["Api__JsonBodyLimitBytes"] = "16384",
            ["Api__MultipartBodyLimitBytes"] = "6291456",
            ["Api__MaximumJsonDepth"] = "16",
            ["PerformanceDiagnostics__Enabled"] = "true",
            ["Logging__LogLevel__Default"] = "Information",
            ["Logging__LogLevel__Microsoft.EntityFrameworkCore"] = "Warning",
            ["Logging__Console__FormatterOptions__SingleLine"] = "true"
        };
        foreach (var (category, permits) in new[] { ("PublicReads", 20000), ("Authentication", 1000),
            ("ReservationWrites", 1000), ("AdminWrites", 100), ("FileUploads", 10) })
        {
            configuration[$"RateLimits__{category}__PermitLimit"] = permits.ToString();
            configuration[$"RateLimits__{category}__WindowSeconds"] = "1";
        }
        foreach (var (name, value) in configuration)
        {
            start.Environment[name] = value;
        }

        _api = Process.Start(start) ?? throw new InvalidOperationException("API process could not start.");
        _stdout = CaptureAsync(_api.StandardOutput, Path.Combine(output, "api-resources.jsonl"), cancellationToken);
        // Exception text can contain credentials. Persist neither stderr nor arbitrary application log lines.
        _stderr = DrainAsync(_api.StandardError, cancellationToken);
        using var readiness = NewClient();
        var ready = false;

        for (var attempt = 0; attempt < 120 && !_api.HasExited; attempt++)
        {
            try
            {
                using var response = await readiness.GetAsync("/health/live", cancellationToken);
                ready = response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                // Kestrel may still be binding its loopback HTTPS listener.
            }

            if (ready)
            {
                break;
            }

            await Task.Delay(500, cancellationToken);
        }

        if (!ready)
        {
            throw new InvalidOperationException("Baseline API failed readiness; raw host errors were suppressed.");
        }

        foreach (var customer in Customers.Take(128))
        {
            Sessions.Add(new BaselineSession(NewClient(), customer, false));
        }

        Staff = new BaselineSession(NewClient(), Admin, true);
        await RenewSessionsAsync(cancellationToken);
        await WriteEnvironmentAsync(output, cancellationToken);
        logger.LogInformation("Baseline ready: one Production API, 1000 projects, 100000 units and 128 active customers");
    }

    public HttpClient NewClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate is not null &&
                CryptographicOperations.FixedTimeEquals(certificate.GetCertHash(HashAlgorithmName.SHA256), _certificateHash!)
        };

        return new HttpClient(handler) { BaseAddress = new Uri(_origin), Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task RenewSessionsAsync(CancellationToken cancellationToken)
    {
        var issuer = new JwtTokenService(Options.Create(_jwt), TimeProvider.System);

        // Fixture-issued tokens retain the normal 15-minute lifetime and pass every real server-side check.
        // Issuance/login throughput is outside this workload; CSRF bootstrap remains real HTTPS I/O.
        foreach (var session in Sessions.Append(Staff))
        {
            var token = session.IsStaff ? issuer.GenerateUserAccessToken(session.Actor, UserRole.Admin)
                : issuer.GenerateCustomerAccessToken(session.Actor);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await session.Client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            session.Credentials = new SessionCredentials(token, json.RootElement.GetProperty("data")
                .GetProperty("requestToken").GetString()!);
        }
    }

    public async Task<SqlConnection> OpenDatabaseAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenDatabaseAsync(cancellationToken);
        var hash = new PasswordHasher();
        var passwordHash = hash.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "aA1!");
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 180;
        command.Parameters.AddWithValue("@hash", passwordHash);
        command.CommandText = """
            ALTER DATABASE SCOPED CONFIGURATION SET LAST_QUERY_PLAN_STATS = ON;
            SET NOCOUNT ON;
            SELECT TOP (100000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n INTO #numbers
            FROM sys.all_objects a CROSS JOIN sys.all_objects b;
            INSERT Cities (Id, Name, CreatedAt)
            SELECT NEWID(), CONCAT(N'City ', n), SYSDATETIMEOFFSET() FROM #numbers WHERE n <= 10;
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Name) AS n INTO #cities FROM Cities;
            INSERT Areas (Id, CityId, Name, CreatedAt)
            SELECT NEWID(), c.Id, CONCAT(N'Area ', x.n), SYSDATETIMEOFFSET()
            FROM #numbers x JOIN #cities c ON c.n = (x.n - 1) % 10 + 1 WHERE x.n <= 100;
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Name) AS n INTO #areas FROM Areas;
            INSERT BaseProjects (Id, AreaId, Name, Description, Size, ProjectType, Price, FloorCount,
                PlannedUnitCount, IsGarage, CreatedAt)
            SELECT NEWID(), a.Id, CONCAT(N'Project ', x.n), REPLICATE(N'Description ', 30), 10000,
                'regular', 100000, 10, 100, 0, SYSDATETIMEOFFSET()
            FROM #numbers x JOIN #areas a ON a.n = (x.n - 1) % 100 + 1 WHERE x.n <= 1000;
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Name) AS n INTO #projects FROM BaseProjects;
            INSERT BaseUnits (Id, ProjectId, Number, MeterPrice, FloorNumber, Size, CreatedAt)
            SELECT NEWID(), p.Id, CONCAT(N'U', x.n), 100, x.n % 10, 100, SYSDATETIMEOFFSET()
            FROM #numbers x JOIN #projects p ON p.n = (x.n - 1) % 1000 + 1;
            INSERT ProjectAmenities (Id, ProjectId, Name, Value, CreatedAt)
            SELECT NEWID(), Id, N'Parking', N'Available', SYSDATETIMEOFFSET() FROM BaseProjects;
            INSERT ProjectImages (Id, ProjectId, ImagePath, AltText, CreatedAt)
            SELECT NEWID(), Id, N'projects/synthetic.jpg', N'Synthetic image', SYSDATETIMEOFFSET() FROM BaseProjects;
            INSERT UnitAmenities (Id, UnitId, Name, Value, CreatedAt)
            SELECT NEWID(), Id, N'Balcony', N'Available', SYSDATETIMEOFFSET() FROM BaseUnits;
            INSERT UnitImages (Id, UnitId, ImagePath, AltText, CreatedAt)
            SELECT NEWID(), Id, N'units/synthetic.jpg', N'Synthetic image', SYSDATETIMEOFFSET() FROM BaseUnits;
            INSERT Customers (Id, FName, LName, Phone, IdentityNumber, Email, HashPassword, CreatedAt)
            SELECT NEWID(), N'Synthetic', CONCAT(N'Customer ', n), CONCAT('010', RIGHT(CONCAT('00000000', n), 8)),
                CONCAT('synthetic-', n), CONCAT('customer', n, '@example.test'), @hash, SYSDATETIMEOFFSET()
            FROM #numbers WHERE n <= 10000;
            INSERT Users (Id, Name, Email, Phone, HashPassword, Role, CreatedAt)
            VALUES (NEWID(), N'Baseline admin', 'admin@example.test', '01100000000', @hash, 'admin', SYSDATETIMEOFFSET());
            INSERT TermsAndConditions (Id, Title, Content, VersionNumber, IsActive, EffectiveDate, CreatedAt)
            VALUES (NEWID(), N'Baseline terms', REPLICATE(N'Synthetic terms. ', 256), 1, 1,
                DATEADD(day, -1, SYSDATETIMEOFFSET()), SYSDATETIMEOFFSET());
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS n INTO #units FROM BaseUnits;
            SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS n INTO #customers FROM Customers;
            INSERT Reservations (Id, CustomerId, UnitId, Number, Status, ReservationDate, CreatedAt)
            SELECT NEWID(), c.Id, u.Id, CONCAT('seed-', x.n), 'cancelled', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
            FROM #numbers x JOIN #units u ON u.n = x.n
            JOIN #customers c ON c.n = (x.n - 1) % 10000 + 1 WHERE x.n <= 20000;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        command.Parameters.Clear();
        command.CommandText = "SELECT Id, ProjectId FROM BaseUnits ORDER BY Id; SELECT Id FROM Customers ORDER BY Id; SELECT Id FROM Users;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            Units.Add((reader.GetGuid(0), reader.GetGuid(1)));
        }

        await reader.NextResultAsync(cancellationToken);
        var customers = new List<Guid>();

        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(reader.GetGuid(0));
        }

        Customers = customers.ToArray();
        await reader.NextResultAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        Admin = reader.GetGuid(0);
    }

    private async Task WriteEnvironmentAsync(string output, CancellationToken cancellationToken)
    {
        await using var connection = await OpenDatabaseAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@VERSION";
        var sqlVersion = (string)(await command.ExecuteScalarAsync(cancellationToken))!;
        var description = new
        {
            StartedUtc = DateTimeOffset.UtcNow,
            ApiInstances = 1,
            ApiPid = _api!.Id,
            Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            LogicalProcessors = Environment.ProcessorCount,
            Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            AvailableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            ServerGc = System.Runtime.GCSettings.IsServerGC,
            SqlVersion = sqlVersion,
            SqlImage = "mcr.microsoft.com/mssql/server:2022-latest",
            ContainerId,
            SqlMaxPoolSize = 100,
            Cache = "DistributedMemoryCache; no feature handler uses it",
            Network = "Windows generator -> loopback HTTPS Kestrel -> Docker Desktop Linux SQL Server mapped TCP port",
            Data = new
            {
                Cities = 10,
                Areas = 100,
                Projects = 1000,
                Units = 100000,
                Customers = 10000,
                ActiveCustomers = 128,
                Staff = 1,
                InitialReservations = 20000,
                ProjectImages = 1000,
                UnitImages = 100000,
                ProjectAmenities = 1000,
                UnitAmenities = 100000
            },
            ReadWriteRatio = "95:5",
            PublicCustomerAdminWriteWeights = new[] { 80, 10, 5, 5 },
            MaximumInFlight = 256,
            MaximumQueued = 256,
            HttpTimeoutSeconds = 5,
            RateLimitsPerSecond = new
            {
                PublicReads = 20000,
                Authentication = 1000,
                ReservationWrites = 1000,
                AdminWrites = 100,
                FileUploads = 10
            },
            Acceptance = new { P95Ms = 200, P99Ms = 500, UnexpectedErrorRateBelow = .01 },
            FixtureSessions = "Trusted service issues JWTs every 10 minutes; normal validation/current-actor checks and real CSRF bootstrap retained"
        };
        await File.WriteAllTextAsync(Path.Combine(output, "environment.json"),
            JsonSerializer.Serialize(description, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
    }

    private static async Task CaptureAsync(StreamReader reader, string path, CancellationToken cancellationToken)
    {
        await using var file = new StreamWriter(path);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            const string marker = "BROUJ_PERFORMANCE ";
            var position = line.IndexOf(marker, StringComparison.Ordinal);

            if (position >= 0)
            {
                await file.WriteLineAsync(line.AsMemory(position + marker.Length), cancellationToken);
                await file.FlushAsync(cancellationToken);
            }
        }
    }

    private static async Task DrainAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is not null)
        {
            // Deliberately discard unstructured host output, including exception text.
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in Sessions)
        {
            session.Client.Dispose();
        }

        Staff?.Client.Dispose();

        if (_api is not null)
        {
            if (!_api.HasExited)
            {
                _api.Kill(true);
            }

            await _api.WaitForExitAsync();
            await Task.WhenAll(_stdout!, _stderr!);
            _api.Dispose();
        }

        // Delete only the generated PFX, never a developer certificate or an existing storage directory.
        var certificate = Path.Combine(_directory, "localhost.pfx");

        if (File.Exists(certificate))
        {
            File.Delete(certificate);
        }

        await _container.DisposeAsync();
        logger.LogInformation("Isolated baseline API and SQL container disposed");
    }
}

internal sealed record SessionCredentials(string Token, string Csrf);

internal sealed class BaselineSession(HttpClient client, Guid actor, bool isStaff)
{
    public HttpClient Client { get; } = client;
    public Guid Actor { get; } = actor;
    public bool IsStaff { get; } = isStaff;
    public volatile SessionCredentials Credentials = new("", "");
}
