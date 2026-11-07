using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using MediatR;

namespace EventHub.Application.Features.Bookings.Queries.GetMyBookings
{
    public sealed class GetMyBookingsQueryHandler
        : IRequestHandler<
            GetMyBookingsQuery,
            Result<PagedResult<BookingListResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public GetMyBookingsQueryHandler(
            IUnitOfWork unitOfWork
            ,ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PagedResult<BookingListResponse>>> Handle(
            GetMyBookingsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == Guid.Empty)
            {
                return UserErrors.Unauthorized;
            }

            if (request.EventId.HasValue &&
                request.EventId.Value == Guid.Empty)
            {
                return Result<PagedResult<BookingListResponse>>.Fail(
                    new Error(
                        "Booking.InvalidEventId",
                        "Event ID must be a valid identifier.",
                        ErrorType.Validation));
            }

            if (request.CreatedFrom.HasValue &&
                request.CreatedTo.HasValue &&
                request.CreatedFrom.Value > request.CreatedTo.Value)
            {
                return Result<PagedResult<BookingListResponse>>.Fail(
                    new Error(
                        "Booking.InvalidDateRange",
                        "CreatedFrom must be earlier than or equal to CreatedTo.",
                        ErrorType.Validation));
            }

            var specification = new UserBookingsSpecification(
                userId,
                request);

            var bookings = await _unitOfWork
                .GetRepository<Booking, Guid>()
                .GetPagedAsync(specification, cancellationToken);

            var bookingResponses = bookings.Items
                .Select(booking => new BookingListResponse
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    EventName = booking.Event.Name,
                    Status = booking.Status,
                    CreatedAt = booking.CreatedAt,
                    ExpiresAt = booking.ExpiresAt,
                    TotalAmount = booking.TotalAmount,
                    TicketQuantity = booking.BookingItems
                        .Sum(item => item.Quantity)
                })
                .ToList();

            var pagedResponse = new PagedResult<BookingListResponse>(
                bookingResponses,
                bookings.PageNumber,
                bookings.PageSize,
                bookings.TotalCount,
                bookings.TotalPages);

            return Result<PagedResult<BookingListResponse>>.Ok(
                pagedResponse);
        }
    }
}