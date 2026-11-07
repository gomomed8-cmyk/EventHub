using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Events;
using MediatR;


namespace EventHub.Application.Features.Events.Commands.Create_Event
{
    public sealed record CreateEventCommand(CreateEventRequest Request) : IRequest<Result<EventResponse>>
    {
    }
}
