using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Booking;
using EventHub.Domain.Enums;
using MediatR;


namespace EventHub.Application.Features.Bookings.Queries.GetEventBookings
{
    public sealed record GetEventBookingsQuery(
    Guid EventId,
    int? PageNumber = 1,
    int? PageSize = 10,
    BookingStatus? Status = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    string? SortBy = "CreatedAt",
    bool SortDescending = true) 
     : IRequest<Result<PagedResult<OrganizerBookingListResponse>>>;

}
