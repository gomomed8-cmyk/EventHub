using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Booking
{
    public class EventBookingSummaryResponse
    {
        public Guid EventId { get; set; }
        public string EventName { get; set; } = default!;
        public EventStatus EventStatus { get; set; }
        public TicketMode TicketMode { get; set; }
        public int Capacity { get; set; }
        public int AvailableQuantity { get; set; }
        public int SoldQuantity { get; set; }

        public int HeldQuantity { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int ExpiredBookings { get; set; }
        public int CancelledBookings { get; set; }

    }
}
