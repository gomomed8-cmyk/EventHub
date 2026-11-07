

namespace EventHub.Application.DTOs.Auth
{
    public record UserEmailInfo(
         Guid UserId,
         string Email,
         string DisplayName);
}
