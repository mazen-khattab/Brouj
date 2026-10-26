using Brouj.Application.Common.Models;
namespace Brouj.Application.Abstractions.Authentication;


public interface IAuthenticationSession
{
    string? ReadRefreshToken();

    void Write(RefreshTokenResult result);

    void Clear();
}
