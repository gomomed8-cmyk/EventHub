
namespace EventHub.Application.DTOs.Email
{
    public class BookingConfirmationEmailModel
    {
        public string CustomerName { get; set; } = string.Empty;

        public Guid BookingId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public DateTime EventStartDate { get; set; }

        public DateTime EventEndDate { get; set; }

        public int TotalTickets { get; set; }

        public decimal TotalAmount { get; set; }

        public List<TicketEmailModel> Tickets { get; set; } = new();
    }

    public class TicketEmailModel
    {
        public int TicketNumber { get; set; }

        public string TicketCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
