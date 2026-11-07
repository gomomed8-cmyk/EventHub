using EventHub.Domain.Common;
using EventHub.Domain.Enums;


namespace EventHub.Domain.Entities
{
    public class Booking: BaseEntity<Guid>
    {
        public Guid UserId { get; set; }

        public Guid EventId { get; set; }
        public Event Event { get; set; } = default!;
        public ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
        public ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? TicketsEmailSentAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public decimal TotalAmount { get; set; }
        public byte[] RowVersion { get; set; } = default!;
        public DateTime? TicketsEmailClaimedAt { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
