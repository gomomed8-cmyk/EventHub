
using EventHub.Application.DTOs.Events;
using MediatR;

namespace EventHub.Application.Features.Events.Commands.UpdateEvent
{
    public sealed record UpdateEventCommand(Guid EventId,UpdateEventRequest UpdateEventRequest): IRequest<Result<EventResponse>>
    {

    }
}
