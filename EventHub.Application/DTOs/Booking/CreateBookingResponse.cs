using EventHub.Domain.Enums;


namespace EventHub.Application.DTOs.Booking
{
    public class CreateBookingResponse
    {
        public Guid BookingId { get; set; }

        public Guid EventId { get; set; }

        public string EventName { get; set; } = default!;

        public BookingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public decimal TotalAmount { get; set; }

        public List<CreateBookingItemResponse> Items { get; set; }
            = new();
    }
}
