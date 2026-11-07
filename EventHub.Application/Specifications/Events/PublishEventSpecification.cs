

using EventHub.Domain.Entities;

namespace EventHub.Application.Specifications.Events
{
    public  class PublishEventSpecification : Specification<Event,Guid>
    {
        public PublishEventSpecification(Guid EventId):base(e => e.Id ==EventId)
        {
            AddInclude(e => e.TicketCategories);
        }
    }
}
