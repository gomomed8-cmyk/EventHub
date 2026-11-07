
using System.ComponentModel.DataAnnotations;
namespace EventHub.Application.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
        [Required]
        [MaxLength(50)]
        public string Password { get; set; } = default!;
    }
}
