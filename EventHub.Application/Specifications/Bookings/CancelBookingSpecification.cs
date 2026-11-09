using EventHub.Domain.Entities;


namespace EventHub.Application.Specifications.Bookings
{
  
        public class CancelBookingSpecification: Specification<Booking, Guid>
        {
            public CancelBookingSpecification(Guid bookingId, Guid? userId)
                : base(b => b.Id == bookingId &&
                !userId.HasValue||b.UserId==userId.Value)
            {
                Includes.Add(b => b.Event.TicketCategories);
                Includes.Add(b => b.BookingItems);
            }
        }
    
}
