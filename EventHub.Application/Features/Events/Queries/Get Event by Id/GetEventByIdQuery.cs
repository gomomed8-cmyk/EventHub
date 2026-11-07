using EventHub.Application.DTOs.Events;
using MediatR;


namespace EventHub.Application.Features.Events.Queries.Get_Event_by_Id
{
    public sealed record GetEventByIdQuery(Guid EventId): IRequest<Result<EventResponse>>
    {

    }
}
