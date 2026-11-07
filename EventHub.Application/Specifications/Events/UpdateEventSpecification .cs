using EventHub.Domain.Entities;
using EventHub.Domain.Enums;


namespace EventHub.Application.Specifications.Events
{
    public class UpdateEventSpecification: Specification<Event, Guid>
    {
        public UpdateEventSpecification(Guid EventId):base(e=>e.Status==EventStatus.Draft && e.Id==EventId)
        {
            AddInclude(e => e.TicketCategories);
        }
    }
}
    