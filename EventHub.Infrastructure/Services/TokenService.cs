using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Auth;
using EventHub.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace EventHub.Infrastructure.Services
{
    internal class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        public TokenService(IOptions<JwtSettings> jWTSettings)
        {
            _jwtSettings = jWTSettings.Value;
        }

        public string GenerateAccessToken(UserTokenInfo userTokenInfo, CancellationToken ct = default)
        {
            var claims = new List<Claim>
{
            new Claim(ClaimTypes.NameIdentifier, userTokenInfo.UserId.ToString()),
            new Claim(ClaimTypes.Email, userTokenInfo.Email),
            new Claim(ClaimTypes.Role, userTokenInfo.Role)
            };
            var secretKey= new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
            (
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );
              return new JwtSecurityTokenHandler().WriteToken(token);


        }

        public string GenerateRefreshToken()
        {
           var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
}
