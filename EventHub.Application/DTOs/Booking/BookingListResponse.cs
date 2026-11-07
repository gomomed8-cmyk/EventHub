using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Booking
{
    public sealed class BookingListResponse
    {
        public Guid BookingId { get; set; }

        public Guid EventId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public BookingStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public decimal TotalAmount { get; set; }

        public int TicketQuantity { get; set; }
    }
}