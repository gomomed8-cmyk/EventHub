using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Bookings
{
    public sealed class GetEventForBookingSpecification
        : Specification<Event, Guid>
    {
        public GetEventForBookingSpecification(Guid eventId)
            : base(e => e.Id == eventId)
        {
            Includes.Add(e => e.TicketCategories);
        }
    }
}