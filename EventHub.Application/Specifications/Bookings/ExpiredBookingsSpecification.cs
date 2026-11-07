

using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Application.Specifications.Bookings
{
    public class ExpiredBookingsSpecification:Specification<Booking,Guid>
    {
        public ExpiredBookingsSpecification(DateTime utcNow) :
            base(b=>b.Status==BookingStatus.Pending && b.ExpiresAt.HasValue && b.ExpiresAt.Value <=utcNow)
        {
            Includes.Add(b => b.BookingItems);
            Includes.Add(b => b.Event);
        }
    }
}
