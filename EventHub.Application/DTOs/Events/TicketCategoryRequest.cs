
using System.ComponentModel.DataAnnotations;


namespace EventHub.Application.DTOs.Events
{
    public class TicketCategoryRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }=default!;

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public int Capacity { get; set; }
    }
}
