

namespace EventHub.Application.DTOs.Auth
{
    public record LoginUserInfo
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string Role {  get; set; } = default!;
    }
}
