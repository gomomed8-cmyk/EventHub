

namespace EventHub.Application.DTOs.Booking
{
    public class CreateBookingItemResponse
    {
        public Guid? TicketCategoryId { get; set; }

        public string CategoryName { get; set; } = default!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
