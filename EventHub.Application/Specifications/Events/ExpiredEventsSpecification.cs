using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Application.Specifications.Events
{
    public class ExpiredEventsSpecification : Specification<Event, Guid>
    {
      public ExpiredEventsSpecification():base(e => e.EndDate <= DateTime.UtcNow && e.Status == EventStatus.Published)
        {
            
        }
    }
}
