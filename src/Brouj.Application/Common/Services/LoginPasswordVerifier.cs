using Brouj.Application.Abstractions.Authentication;
namespace Brouj.Application.Common.Services;

public sealed class LoginPasswordVerifier
{
    private readonly IPasswordHasher _hasher;
    private readonly string _dummyHash;

    public LoginPasswordVerifier(IPasswordHasher hasher)
    {
        _hasher = hasher;
        _dummyHash = hasher.Hash(Guid.NewGuid().ToString("N"));
    }

    public bool Verify(string password, string? storedHash)
    {
        // Missing and deleted accounts still perform the trusted hasher's expensive verification.
        var valid = _hasher.Verify(password, storedHash ?? _dummyHash);

        return storedHash is not null && valid;
    }
}
