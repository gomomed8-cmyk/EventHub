using EventHub.Domain.Common;


namespace EventHub.Domain.Entities
{
    public class TicketCategory: BaseEntity<Guid>
    {
       

        public string Name { get; set; } = default!;

        public Guid EventId { get; set; }
        public Event Event { get; set; }= default!;
        public int Capacity { get; set; }
        public int AvailableQuantity { get; set; }
        public decimal Price { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
