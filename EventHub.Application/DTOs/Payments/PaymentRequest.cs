using System.ComponentModel.DataAnnotations;

namespace EventHub.Application.DTOs.Payments
{
    public sealed class PaymentRequest
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Street { get; set; }

        [StringLength(50)]
        public string? Building { get; set; }

        [StringLength(50)]
        public string? Apartment { get; set; }

        [StringLength(50)]
        public string? Floor { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string Country { get; set; } = "EG";

        [StringLength(20)]
        public string? PostalCode { get; set; }
    }
}