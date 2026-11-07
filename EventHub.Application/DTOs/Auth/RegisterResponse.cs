
namespace EventHub.Application.DTOs.Auth
{
    public record RegisterResponse
    {
        public Guid UserId { get; set; }
        public string DisplayName { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string AccessToken { get; init; } = default!;
        public string RefreshToken { get; init; } = default!;
    }
}
