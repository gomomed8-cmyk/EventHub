using EventHub.Domain.Common;
using EventHub.Domain.Enums;


namespace EventHub.Domain.Entities
{
    public class Event: BaseEntity<Guid>
    {

       public Guid OrganizerId { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public ICollection<TicketCategory> TicketCategories { get; set; } = new List<TicketCategory>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public EventStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public EventCategory Category { get; set; }
        public TicketMode TicketMode { get; set; }
        public string? DuplicateFingerprint { get; set; } = default!;
        public int AvailableQuantity { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
