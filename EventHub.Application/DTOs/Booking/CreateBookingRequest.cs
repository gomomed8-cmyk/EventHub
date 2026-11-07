using EventHub.Domain.Entities;
using System.ComponentModel.DataAnnotations;


namespace EventHub.Application.DTOs.Booking
{
    public class CreateBookingRequest
    {
        [Required]
        public Guid EventId { get; set; }

        public List<CreateBookingItemRequest> Items { get; set; }
            = new();

    }
}
