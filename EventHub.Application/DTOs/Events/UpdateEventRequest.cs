

using EventHub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EventHub.Application.DTOs.Events
{
    public class UpdateEventRequest
    {

        [MaxLength(100)]
        public string? Name { get; set; } 
        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
       
        public DateTime? EndDate { get; set; }

        public decimal? Price { get; set; }
        public int? Capacity { get; set; }
        public EventCategory? Category { get; set; }
        public TicketMode? TicketMode { get; set; }
        public IReadOnlyList<TicketCategoryRequest>? TicketCategories { get; set; }

    }
}
