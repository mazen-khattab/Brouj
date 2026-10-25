using Brouj.Application.Abstractions.Authentication;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureHasher : IPasswordHasher
{
    public string Hash(string password) => "synthetic-hash:" + password;

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);

    public bool NeedsRehash(string passwordHash) => false;
}
