using System.Reflection;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brouj.API.IntegrationTests.Support;

public sealed class Part8ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _recordDispatch;
    private readonly Dictionary<string, string?> _configuration;
    internal ApiTestDb Database { get; } = new();
    internal ApiTestLogProvider Logs { get; } = new();
    internal ApiTestRefreshTokens RefreshTokens { get; } = new();
    public ISender Sender { get; } = DispatchProxy.Create<ISender, RecordingSender>();
    public RecordingSender Recording => (RecordingSender)Sender;
    public Guid CustomerId { get; } = Guid.NewGuid();
    public Guid OtherCustomerId { get; } = Guid.NewGuid();
    public Guid AdminId { get; } = Guid.NewGuid();
    public Guid SuperAdminId { get; } = Guid.NewGuid();

    public Part8ApiFactory() : this(true)
    {
    }

    internal Part8ApiFactory(bool recordDispatch, Dictionary<string, string?>? overrides = null)
    {
        _recordDispatch = recordDispatch;
        _configuration = ApiTestConfiguration.Create();

        foreach (var pair in overrides ?? [])
        {
            _configuration[pair.Key] = pair.Value;
        }

        Database.Customers.AddRange(
            new Customer { Id = CustomerId, Email = "customer@example.test" },
            new Customer { Id = OtherCustomerId, Email = "other@example.test" });
        Database.Users.AddRange(
            new User { Id = AdminId, Name = "Admin", Role = UserRole.Admin },
            new User { Id = SuperAdminId, Name = "SuperAdmin", Role = UserRole.SuperAdmin });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(_configuration));

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_configuration[HostDefaults.EnvironmentKey]!);
        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TransportProbeController).Assembly);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddLogging(logging => logging.AddProvider(Logs));
            services.RemoveAll<IApplicationDbContext>();
            services.AddSingleton<IApplicationDbContext>(Database);
            services.RemoveAll<IApplicationTransactionManager>();
            services.AddSingleton<IApplicationTransactionManager, ApiTestTransactions>();
            services.RemoveAll<IRefreshTokenService>();
            services.AddSingleton<IRefreshTokenService>(provider =>
            {
                RefreshTokens.Jwt = provider.GetRequiredService<IJwtTokenService>();
                return RefreshTokens;
            });

            if (_recordDispatch)
            {
                services.RemoveAll<ISender>();
                services.AddSingleton(Sender);
            }
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://brouj.test"), AllowAutoRedirect = false, HandleCookies = false
    });

    public string Credential(string actor)
    {
        var jwt = Services.GetRequiredService<IJwtTokenService>();

        return actor switch
        {
            "CustomerOnly" or "MemberOnly" => jwt.GenerateCustomerAccessToken(CustomerId),
            "OtherCustomer" => jwt.GenerateCustomerAccessToken(OtherCustomerId),
            "AdminOnly" or "StaffOnly" => jwt.GenerateUserAccessToken(AdminId, UserRole.Admin),
            "SuperAdminOnly" => jwt.GenerateUserAccessToken(SuperAdminId, UserRole.SuperAdmin),
            _ => string.Empty
        };
    }
}
