using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Auth;
using EventHub.Application.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace EventHub.Application.Features.Auth.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandHandler
        : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
    {
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ITokenService _tokenService;
        private readonly IIdentityService _identityService;
        private readonly IOptions<RefreshTokenSettings> _settings;

        public RefreshTokenCommandHandler(
            IRefreshTokenStore refreshTokenStore,
            ITokenService tokenService,
            IIdentityService identityService,
            IOptions<RefreshTokenSettings> options)
        {
            _refreshTokenStore = refreshTokenStore;
            _tokenService = tokenService;
            _identityService = identityService;
            _settings = options;
        }

        public async Task<Result<LoginResponse>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _refreshTokenStore.GetUserIdByRefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);

            if (string.IsNullOrEmpty(user))
            {
                return Result<LoginResponse>.Fail(
                    new Error(
                        "UserNotFound",
                        "User not found."));
            }

            var getRefresh = await _refreshTokenStore.GetRefreshTokenAsync(
                user,
                cancellationToken);

            if (string.IsNullOrEmpty(getRefresh))
            {
                return Result<LoginResponse>.Fail(
                    new Error(
                        "RefreshTokenNotFound",
                        "Refresh token not found."));
            }

            if (getRefresh != request.RefreshToken)
            {
                return Result<LoginResponse>.Fail(
                    new Error(
                        "InvalidRefreshToken",
                        "Invalid refresh token."));
            }

            var result = await _identityService.GetUserByIdAsync(
                Guid.Parse(user),
                cancellationToken);

            if (result.IsFailure)
            {
                return Result<LoginResponse>.Fail(
                    new Error(
                        "UserNotFound",
                        "User not found."));
            }

            var accessToken = _tokenService.GenerateAccessToken(
                new UserTokenInfo
                {
                    UserId = result.Value.UserId,
                    Email = result.Value.Email,
                    Role = result.Value.Role
                },
                cancellationToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                return Result<LoginResponse>.Fail(
                    new Error(
                        "InvalidToken",
                        "The token is not valid."));
            }

            var refresh = _tokenService.GenerateRefreshToken();
            await _refreshTokenStore.DeleteRefreshTokenAsync(
                   result.Value.UserId.ToString(),
                      request.RefreshToken,
                     cancellationToken);
       
            var storeRefresh = await _refreshTokenStore.StoreRefreshTokenAsync(
                result.Value.UserId.ToString(),
                refresh,
                TimeSpan.FromDays(_settings.Value.ExpirationDays),
                cancellationToken);

            if (!storeRefresh)
            {
                return Result<LoginResponse>.Fail(
                    new Error(
                        "RefreshTokenStoreError",
                        "Failed to store refresh token."));
            }

            return Result<LoginResponse>.Ok(
                new LoginResponse
                {
                    DisplayName = result.Value.DisplayName,
                    Email = result.Value.Email,
                    AccessToken = accessToken,
                    RefreshToken = refresh
                });
        }
    }
}