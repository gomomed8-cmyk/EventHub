using EventHub.Application.DTOs.Auth;

namespace EventHub.Application.Contracts
{
    public interface ITokenService
    {
        string GenerateAccessToken(UserTokenInfo userTokenInfo, CancellationToken ct = default);
        string GenerateRefreshToken();
    }
}
