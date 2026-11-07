using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Booking;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Bookings.Queries.GetMyBookings
{
    public sealed record GetMyBookingsQuery(
        BookingStatus? Status = null,
        Guid? EventId = null,
        DateTime? CreatedFrom = null,
        DateTime? CreatedTo = null,
        int? PageNumber = 1,
        int? PageSize = 10,
        string? SortBy = "CreatedAt",
        bool SortDescending = true
    ) : IRequest<Result<PagedResult<BookingListResponse>>>;
}