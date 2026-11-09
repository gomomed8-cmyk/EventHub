using EventHub.Domain.Entities;


namespace EventHub.Application.Specifications.Bookings
{
    public class GetBookingByIdSpecification:Specification<Booking,Guid>
    {
        public GetBookingByIdSpecification(Guid bookingId,Guid? userId)
            :base(b=>b.Id==bookingId &&
            !userId.HasValue || b.UserId == userId.Value)
        {
            Includes.Add(b => b.Event);
            Includes.Add(b => b.BookingItems);
            Includes.Add(b => b.Event.TicketCategories);
        }
    }
}
