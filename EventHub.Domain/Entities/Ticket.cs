using EventHub.Domain.Common;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities
{
    public class Ticket : BaseEntity<Guid>
    {
        public Guid BookingItemId { get; set; }

        public BookingItem BookingItem { get; set; } = default!;

        public string TicketCode { get; set; } = default!;
        public int TicketNumber { get; set; }
        public TicketStatus Status { get; set; }

        public DateTime IssuedAt { get; set; }

        public DateTime? UsedAt { get; set; }
    }
}