using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Auth;
using EventHub.Application.Settings;
using MediatR;
using Microsoft.Extensions.Options;


namespace EventHub.Application.Features.Auth.Commands.Register
{
    public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IOptions<RefreshTokenSettings> _refreshTokenSettings;

        public RegisterCommandHandler(IIdentityService identityService,
            ITokenService tokenService,
            IRefreshTokenStore refreshTokenStore,IOptions<RefreshTokenSettings> options)
        {

            _identityService = identityService;
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _refreshTokenSettings = options;
        }

        public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var user= await _identityService.CreateUserAsync(request.Request, cancellationToken);
            if (!user.IsSuccess)
            {
                return Result<RegisterResponse>.Fail(user.Errors);
            }
             var userTokenInfo =new UserTokenInfo
            {
                UserId = user.Value,
                Email =request.Request.Email,
                Role = "User"
             };
            var accessToken = _tokenService.GenerateAccessToken(userTokenInfo);
            var refreshToken = _tokenService.GenerateRefreshToken();
           var storeRefreshTokenResult = await _refreshTokenStore.StoreRefreshTokenAsync(userTokenInfo.UserId.ToString(), refreshToken,TimeSpan.FromDays(_refreshTokenSettings.Value.ExpirationDays), cancellationToken);
          if (!storeRefreshTokenResult)
            {
               return Result<RegisterResponse>.Fail(new Error("RefreshTokenStoreError", "Failed to store refresh token."));
            }

            var response = new RegisterResponse
            {
                UserId = userTokenInfo.UserId,
                DisplayName = request.Request.DisplayName,
                Email = userTokenInfo.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return Result<RegisterResponse>.Ok(response);


        }
    }
}