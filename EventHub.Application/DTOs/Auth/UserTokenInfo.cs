

namespace EventHub.Application.DTOs.Auth
{
    public class UserTokenInfo
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
