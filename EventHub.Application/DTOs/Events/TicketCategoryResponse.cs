

namespace EventHub.Application.DTOs.Events
{
    public class TicketCategoryResponse
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
