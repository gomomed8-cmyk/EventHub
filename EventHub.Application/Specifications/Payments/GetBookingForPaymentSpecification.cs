using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Payments
{
    public sealed class GetBookingForPaymentSpecification : Specification<Booking, Guid>
    {
        public GetBookingForPaymentSpecification(Guid bookingId,Guid userId): base(b => b.Id == bookingId && b.UserId == userId)
        {
            Includes.Add(b => b.Event.TicketCategories);

            Includes.Add(b => b.BookingItems);
        }
    }
}