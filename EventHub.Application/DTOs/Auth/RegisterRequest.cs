using System.ComponentModel.DataAnnotations;
namespace EventHub.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(50)]
        public string DisplayName { get; set; } = default!;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
        [Required]
        [MaxLength(50)]
        public string Password { get; set; } = default!;
        [Required]
        [RegularExpression(
     @"^(010|011|012|015)[0-9]{8}$",
     ErrorMessage = "Phone number must be an 11-digit Egyptian mobile number starting with 010, 011, 012, or 015."
 )]
        public string Phone { get; set; } = default!;
        [Required]
        [MaxLength(100)]
        public string Address { get; set; } = default!;
    }
}
