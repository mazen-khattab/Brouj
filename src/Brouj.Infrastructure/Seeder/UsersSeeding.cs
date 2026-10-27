using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Utils;
using Brouj.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.Seeder;

/// <summary>Creates the explicitly configured first administrator without changing existing accounts.</summary>
public sealed class UsersSeeding
{
    private readonly IApplicationDbContext _db;
    private readonly IApplicationTransactionManager _transactions;
    private readonly IPasswordHasher _hasher;
    private readonly IOptions<InitialAdminOptions> _options;
    private readonly ILogger<UsersSeeding> _logger;

    public UsersSeeding(
        IApplicationDbContext db, 
        IApplicationTransactionManager transactions, 
        IPasswordHasher hasher,
        IOptions<InitialAdminOptions> options, 
        ILogger<UsersSeeding> logger)
    {
        _db = db;
        _transactions = transactions;
        _hasher = hasher;
        _options = options;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = _options.Value;

            if (!options.Enabled)
            {
                _logger.LogDebug("Initial administrator provisioning is disabled.");
                return;
            }

            if (!InitialAdminOptions.IsValid(options))
            {
                throw new InvalidOperationException("Enabled InitialAdmin settings are incomplete or invalid.");
            }

            if (await _db.Users.AsNoTracking().AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Initial administrator provisioning skipped because staff accounts already exist.");
                return;
            }

            // Hashing is intentionally outside the transaction: keep the database lock short.
            var hash = _hasher.Hash(options.Password);
            var created = await _transactions.ExecuteAsync(async token =>
            {
                // All instances use the configured bootstrap ID. HOLDLOCK also protects an absent key,
                // so two instances starting against an empty database cannot provision twice.
                await _db.Users.FromSqlInterpolated(
                        $"SELECT * FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {options.Id}")
                    .AsNoTracking().Select(user => user.Id).SingleOrDefaultAsync(token);

                if (await _db.Users.AsNoTracking().AnyAsync(token))
                {
                    return false;
                }

                _db.Users.Add(new User
                {
                    Id = options.Id,
                    Name = options.Name.Trim(),
                    Email = InputRules.NormalizeEmail(options.Email),
                    Phone = options.Phone,
                    Role = options.Role!.Value,
                    HashPassword = hash
                });

                await _db.SaveChangesAsync(token);

                return true;
            }, cancellationToken);

            _logger.LogInformation("Initial administrator provisioning finished; created account: {Created}.", created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Bootstrap runs before the API error middleware. Never let a provider exception expose
            // configured credentials through the host's startup logs, and never continue after failure.
            _logger.LogError("Initial administrator provisioning failed; application startup will stop.");

            throw new InvalidOperationException("Initial administrator provisioning failed.");
        }
    }
}
