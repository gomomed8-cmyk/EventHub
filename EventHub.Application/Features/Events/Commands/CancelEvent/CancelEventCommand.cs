

using MediatR;

namespace EventHub.Application.Features.Events.Commands.CancelEvent
{
    public sealed record CancelEventCommand(Guid EventId): IRequest<Result<Unit>>
    {
    }
}
