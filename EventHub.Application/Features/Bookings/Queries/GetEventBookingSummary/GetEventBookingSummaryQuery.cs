using EventHub.Application.DTOs.Booking;
using MediatR;

namespace EventHub.Application.Features.Bookings.Queries.GetEventBookingSummary
{
    public sealed record GetEventBookingSummaryQuery(Guid EventId):IRequest<Result<EventBookingSummaryResponse>>
    {

    }
}
