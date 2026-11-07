
namespace EventHub.Application.DTOs.Auth
{
    public record LoginResponse
    {
        public string DisplayName { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string Role { get; init; } = default!;
        public string AccessToken { get; init; } = default!;
        public string RefreshToken { get; init; } = default!;
    }
}
