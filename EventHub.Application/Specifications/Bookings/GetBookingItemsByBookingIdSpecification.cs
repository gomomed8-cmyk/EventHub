
using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Bookings
{
    public sealed class GetBookingItemsByBookingIdSpecification
        : Specification<BookingItem, Guid>
    {
        public GetBookingItemsByBookingIdSpecification(Guid bookingId)
            : base(item => item.BookingId == bookingId)
        {
        }
    }
}
