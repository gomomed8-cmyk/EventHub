using EventHub.Application.Contracts;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventHub.Infrastructure.Services
{
    internal class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

                return Guid.TryParse(userId, out var id)
                    ? id
                    : Guid.Empty;
            }
        }
    }
}