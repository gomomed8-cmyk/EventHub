

using System.ComponentModel.DataAnnotations;

namespace EventHub.Application.DTOs.Booking
{
    public class CreateBookingItemRequest
    {
        
        public Guid? TicketCategoryId { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
