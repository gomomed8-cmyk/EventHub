
using EventHub.Domain.Enums;
using System.ComponentModel.DataAnnotations;


namespace EventHub.Application.DTOs.Events
{
    public class CreateEventRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }=default!;
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }=default!;

        public DateTime StartDate { get; set; }
     
        public DateTime EndDate { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value.")]
        public decimal Price { get; set; }
        public int Capacity { get; set; }

        [EnumDataType(typeof(EventCategory), ErrorMessage = "Invalid event category.")]
        public EventCategory Category { get; set; }

        public TicketMode TicketMode { get; set; } = TicketMode.GeneralAdmission;
        public IReadOnlyList<TicketCategoryRequest> TicketCategories { get; set; } = new List<TicketCategoryRequest>();

    }
}
