using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Events;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Events.Queries.GetMyEvents
{
    public sealed record GetMyEventsQuery(string? SearchTerm = null, EventCategory? Category = null,
        DateTime? StartDateFrom = null, DateTime? StartDateTo = null, int? PageNumber = 1, int? PageSize = 10
        , string? SortBy = "StartDate", bool SortDescending = false)
        : IRequest<Result<PagedResult<EventListResponse>>>
    {

    }
}
