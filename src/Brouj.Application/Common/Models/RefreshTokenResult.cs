using System;
using System.Collections.Generic;
using System.Text;

namespace Brouj.Application.Common.Models
{
    /// <summary>A successful refresh operation. Treat both credentials as secrets and never log this object.</summary>
    public sealed class RefreshTokenResult(string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        /// <summary>The short-lived signed access credential.</summary>
        public string AccessToken { get; } = accessToken;
        /// <summary>The opaque credential returned once to the client; only its digest is persisted.</summary>
        public string RefreshToken { get; } = refreshToken;
        /// <summary>The absolute refresh-session deadline, unchanged by rotation.</summary>
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        /// <summary>Prevents incidental string formatting from exposing credentials.</summary>
        public override string ToString() => "RefreshTokenResult [redacted]";
    }
}
