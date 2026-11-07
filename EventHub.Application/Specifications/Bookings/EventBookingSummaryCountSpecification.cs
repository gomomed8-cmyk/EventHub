using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Bookings
{
    public sealed class EventBookingSummaryCountSpecification:Specification<Booking, Guid>
    {
        public EventBookingSummaryCountSpecification(Guid eventId):base(b => b.EventId == eventId)
        {
            
            ApplyPaging(1, 1);
        }
    }
}