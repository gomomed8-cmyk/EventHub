

using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Events
{
    
   public class EventResponse
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public EventStatus Status { get; set; }
        public decimal? Price { get; set; }
        public EventCategory? Category { get; set; }
        public TicketMode? TicketMode { get; set; }
        public int? AvailableQuantity { get; set; }
        public IReadOnlyList<TicketCategoryResponse> TicketCategories { get; set; }
                                                 = new List<TicketCategoryResponse>();
    }
   
}
