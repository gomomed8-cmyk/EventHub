

using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Booking
{
    public sealed class OrganizerBookingListResponse
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public string CustomerName { get; set; } = default!;
        public string CustomerEmail { get; set; } = default!;
        public BookingStatus BookingStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }
        public decimal TotalAmount { get; set; }
        public int TicketQuantity { get; set; }
        public List<CreateBookingItemResponse> Items { get; set; } = new();
    }
}
