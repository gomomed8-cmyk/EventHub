
using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Events;

public sealed class DuplicateEventSpecification
    : Specification<Event, Guid>
{
    public DuplicateEventSpecification(Event eventEntity)
        : base(e =>
            e.Id != eventEntity.Id &&
            e.OrganizerId == eventEntity.OrganizerId &&
            e.Name == eventEntity.Name &&
            e.Description == eventEntity.Description &&
            e.StartDate == eventEntity.StartDate &&
            e.EndDate == eventEntity.EndDate &&
            e.Category == eventEntity.Category &&
            e.TicketMode == eventEntity.TicketMode &&
            e.Price == eventEntity.Price &&
            e.Capacity == eventEntity.Capacity)
    {
        AddInclude(e => e.TicketCategories);
    }
}
