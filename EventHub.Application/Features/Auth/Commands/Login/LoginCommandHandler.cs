using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Auth;
using EventHub.Application.Settings;
using MediatR;
using Microsoft.Extensions.Options;


namespace EventHub.Application.Features.Auth.Commands.Login
{
    public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
    {
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly IIdentityService _identityService;
        private readonly IOptions<RefreshTokenSettings> _refreshTokenSettings;

        public LoginCommandHandler(ITokenService tokenService,IIdentityService identityService,
            IRefreshTokenStore refreshTokenStore, IOptions<RefreshTokenSettings> options
           ) 
        { 
            _tokenService = tokenService;
            _refreshTokenStore = refreshTokenStore;
            _identityService = identityService;
            _refreshTokenSettings = options;

        }
        
        
        public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _identityService.LoginAsync(request.Request.Email,request.Request.Password,cancellationToken);
          if(user.IsFailure)
            {
                return Result<LoginResponse>.Fail(user.Errors);
            }
          //return Result<LoginResponse>.Ok(user.Value);
         var accessToken=_tokenService.GenerateAccessToken(
             new UserTokenInfo
             {
                 UserId=user.Value.UserId,
                 Email=user.Value.Email,
                 Role=user.Value.Role
             }
             ,cancellationToken);

            if(string.IsNullOrEmpty(accessToken))
            {
                return Result<LoginResponse>.Fail(new Error("Invalid Token","The token is not valid"));
            }

           
           var refreshToken= _tokenService.GenerateRefreshToken();

            var getToken = await _refreshTokenStore.StoreRefreshTokenAsync(user.Value.UserId.ToString(), refreshToken, TimeSpan.FromDays(_refreshTokenSettings.Value.ExpirationDays), cancellationToken);
            if(!getToken)
            {
                return Result<LoginResponse>.Fail(new Error("RefreshTokenStoreError", "Failed to store refresh token."));

            }
            var repo = new LoginResponse
            {
                DisplayName=user.Value.DisplayName,
                Email=user.Value.Email,
                Role=user.Value.Role,
                AccessToken =accessToken,
                RefreshToken=refreshToken
            };
            return Result<LoginResponse>.Ok(repo);
            
        }
    }
}
