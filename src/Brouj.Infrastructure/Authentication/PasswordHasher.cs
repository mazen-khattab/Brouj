using System.Security.Cryptography;
using System.Text;
using Brouj.Application.Abstractions.Authentication;
using Isopoh.Cryptography.Argon2;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Uses the trusted Argon2id implementation with an independent random salt per password.</summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int MemoryKiB = 19 * 1024;
    private const int Iterations = 2;
    private const int Parallelism = 1;

    /// <summary>Hashes a password with Argon2id v19, 19 MiB, two iterations, one lane, and a fresh 16-byte salt.</summary>
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        var bytes = Encoding.UTF8.GetBytes(password);
        try
        {
            var config = new Argon2Config
            {
                Type = Argon2Type.HybridAddressing, 
                Version = Argon2Version.Nineteen,
                MemoryCost = MemoryKiB, 
                TimeCost = Iterations, 
                Lanes = Parallelism, 
                Threads = Parallelism,
                Salt = RandomNumberGenerator.GetBytes(16), 
                Password = bytes, 
                HashLength = 32
            };
            return Argon2.Hash(config);
        }
        finally
        {
            // The caller's immutable string cannot be cleared, but our temporary UTF-8 copy can.
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    /// <summary>Rejects malformed or excessive-cost hashes before invoking trusted constant-time verification.</summary>
    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || !TryReadConfig(passwordHash, out _)) return false;
        // Verification and the constant-time digest comparison belong to the cryptography library.
        return Argon2.Verify(passwordHash, password);
    }

    /// <summary>Identifies older/weaker parameters for upgrading after a successful password verification.</summary>
    public bool NeedsRehash(string passwordHash) =>
        !TryReadConfig(passwordHash, out var config) ||
        config.Type != Argon2Type.HybridAddressing || config.Version != Argon2Version.Nineteen ||
        config.MemoryCost < MemoryKiB || config.TimeCost < Iterations || config.Lanes != Parallelism ||
        config.HashLength < 32 || config.Salt is not { Length: >= 16 };

    private static bool TryReadConfig(string? encoded, out Argon2Config config)
    {
        config = new Argon2Config();
        if (string.IsNullOrEmpty(encoded) || encoded.Length > 256) return false;
        try
        {
            var decoded = config.DecodeString(encoded, out var digest);
            using (digest)
            {
                // Bound work before trusting cost parameters in a stored hash. These ceilings allow
                // stronger policies without accepting corrupt hashes that request unbounded resources.
                return decoded && digest is not null && config.MemoryCost is >= 8 and <= 262144 &&
                    config.TimeCost is >= 1 and <= 10 && config.Lanes is >= 1 and <= 16 &&
                    config.MemoryCost >= 8 * config.Lanes;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
        {
            return false;
        }
    }
}
