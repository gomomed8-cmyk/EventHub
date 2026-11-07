namespace EventHub.Application.DTOs.Booking
{
    public sealed class EventBookingSummaryStats
    {
        public int TotalBookings { get; set; }

        public int PendingBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int ExpiredBookings { get; set; }

        public int CancelledBookings { get; set; }

        public int SoldQuantity { get; set; }

        public int HeldQuantity { get; set; }
    }
}