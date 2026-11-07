using EventHub.Domain.Common;


namespace EventHub.Domain.Entities
{
    public class BookingItem: BaseEntity<Guid>
    {
        public Guid BookingId { get; set; }
        public Booking Booking { get; set; } = default!;
        public Guid? TicketCategoryId { get; set; }
        public TicketCategory? TicketCategory { get; set; } = default!;
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int Quantity { get; set; }
    }
}
