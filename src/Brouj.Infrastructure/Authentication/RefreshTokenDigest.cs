using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Validates opaque credentials and converts them to the only representation allowed in storage.</summary>
public static class RefreshTokenDigest
{
    /// <summary>Accepts exactly 32 random bytes in canonical Base64url form, returning a SHA-256 hexadecimal digest.</summary>
    /// <remarks>Neither the input nor its digest may be written to logs.</remarks>
    public static bool TryHash(string? token, out string digest)
    {
        digest = "";
        if (token is not { Length: 43 } || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
        {
            return false;
        }

        var bytes = Base64UrlEncoder.DecodeBytes(token);
        if (bytes.Length != 32 || Base64UrlEncoder.Encode(bytes) != token)
        {
            return false;
        }

        // Tokens have 256 bits of entropy: a fast digest is appropriate; password hashing is unnecessary.
        digest = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
        return true;
    }
}
