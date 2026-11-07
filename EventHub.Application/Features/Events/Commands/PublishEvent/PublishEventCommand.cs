using EventHub.Application.DTOs.Events;
using MediatR;


namespace EventHub.Application.Features.Events.Commands.PublishEvent
{
    public sealed record PublishEventCommand(Guid EventId): IRequest<Result<PublishEventCommandResponse>>
    {
    }
}
