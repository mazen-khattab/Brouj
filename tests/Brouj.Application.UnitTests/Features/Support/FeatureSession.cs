using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureSession : IAuthenticationSession
{
    public string? Token { get; set; }

    public RefreshTokenResult? Written { get; private set; }

    public bool Cleared { get; private set; }

    public string? ReadRefreshToken() => Token;

    public void Write(RefreshTokenResult result) => Written = result;

    public void Clear() => Cleared = true;
}
