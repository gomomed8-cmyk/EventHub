

using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Events
{
    public class EventListResponse
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventCategory? Category { get; set; }
        public TicketMode TicketMode { get; set; }
        public decimal? Price { get; set; }
        public decimal? MinTicketPrice { get; set; }
        public decimal? MaxTicketPrice { get;set; }
        public int AvailableQuantity { get; set; }

    }
}
