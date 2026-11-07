

using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Events
{
    public class PublishEventCommandResponse
    {
        public Guid EventId { get; set; }
        public EventStatus Status { get; set; }
    }
}
