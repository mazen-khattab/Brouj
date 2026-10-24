using BenchmarkDotNet.Attributes;
using Brouj.Infrastructure.Authentication;

namespace Brouj.PasswordBenchmarks;

/// <summary>Measures the real approved Argon2id implementation without reducing its security parameters.</summary>
[MemoryDiagnoser]
public class PasswordBenchmarks
{
    private readonly PasswordHasher _hasher = new();
    private string _hash = "";
    // Synthetic benchmark input only; never run benchmarks against a real user's credentials.
    private const string Password = "Synthetic-Benchmark-Password-2026!";

    /// <summary>Precomputes a hash so verification measurements exclude hashing setup.</summary>
    [GlobalSetup]
    public void Setup() => _hash = _hasher.Hash(Password);
    /// <summary>Includes a fresh salt and the complete 19 MiB / 2 iteration Argon2id operation.</summary>
    [Benchmark]
    public string Hash() => _hasher.Hash(Password);
    /// <summary>Measures verification of a matching password, including safe hash-parameter parsing.</summary>
    [Benchmark]
    public bool Verify() => _hasher.Verify(Password, _hash);
    /// <summary>Confirms the cost of rejecting a wrong password without bypassing the hash work.</summary>
    [Benchmark]
    public bool VerifyWrongPassword() => _hasher.Verify("Synthetic-Wrong-Password", _hash);
}
