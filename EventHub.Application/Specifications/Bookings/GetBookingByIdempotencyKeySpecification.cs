using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Bookings
{
    public sealed class GetBookingByIdempotencyKeySpecification
        : Specification<Booking, Guid>
    {
        public GetBookingByIdempotencyKeySpecification(
            Guid userId,
            string idempotencyKey)
            : base(b =>
                b.UserId == userId &&
                b.IdempotencyKey == idempotencyKey)
        {
            Includes.Add(b => b.Event);
            Includes.Add(b => b.BookingItems);
        }
    }
}