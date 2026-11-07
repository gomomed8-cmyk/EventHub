

using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Bookings
{
    public sealed class GetBookingForConfirmationEmailSpecification:Specification<Booking,Guid>
    {
        public GetBookingForConfirmationEmailSpecification(Guid bookingId)
       : base(b => b.Id == bookingId)
        {
            AddInclude(b => b.Event);
            AddIncludePath("BookingItems.Tickets");
        }
    }
}
