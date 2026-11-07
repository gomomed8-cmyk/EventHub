using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
namespace EventHub.Application.Specifications.Events
{
    internal class EventByIdSpecification : Specification<Event, Guid>
    {
        public EventByIdSpecification(Guid EventId) : base(e => e.Id == EventId && e.Status == EventStatus.Published)
        {

            AddInclude(e => e.TicketCategories);
        }

    }
}
