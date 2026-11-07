using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using MediatR;

namespace EventHub.Application.Features.Bookings.Queries.GetEventBookings
{
    public sealed class GetEventBookingsQueryHandler
        : IRequestHandler<
            GetEventBookingsQuery,
            Result<PagedResult<OrganizerBookingListResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICustomerInfoLookup _customerInfoLookup;

        public GetEventBookingsQueryHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ICustomerInfoLookup customerInfoLookup)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _customerInfoLookup = customerInfoLookup;
        }

        public async Task<Result<PagedResult<OrganizerBookingListResponse>>> Handle(
            GetEventBookingsQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == Guid.Empty)
            {
                return Result<PagedResult<OrganizerBookingListResponse>>.Fail(
                    new Error(
                        "User.Unauthorized",
                        "The current user is not authenticated.",
                        ErrorType.Unauthorized));
            }

            if (request.EventId == Guid.Empty)
            {
                return Result<PagedResult<OrganizerBookingListResponse>>.Fail(
                    new Error(
                        "Event.InvalidId",
                        "A valid event ID is required.",
                        ErrorType.Validation));
            }

            if (request.CreatedFrom.HasValue &&
                request.CreatedTo.HasValue &&
                request.CreatedFrom.Value > request.CreatedTo.Value)
            {
                return Result<PagedResult<OrganizerBookingListResponse>>.Fail(
                    new Error(
                        "Booking.InvalidDateRange",
                        "CreatedFrom must be earlier than or equal to CreatedTo.",
                        ErrorType.Validation));
            }

            var eventRepository =
                _unitOfWork.GetRepository<Event, Guid>();

            var eventEntity = await eventRepository.GetByIdAsync(
                request.EventId,
                cancellationToken);

            if (eventEntity is null)
            {
                return Result<PagedResult<OrganizerBookingListResponse>>.Fail(
                    new Error(
                        "Event.NotFound",
                        "The requested event was not found.",
                        ErrorType.NotFound));
            }

            if (eventEntity.OrganizerId != userId)
            {
                return Result<PagedResult<OrganizerBookingListResponse>>.Fail(
                    new Error(
                        "Event.Forbidden",
                        "You are not allowed to view bookings for this event.",
                        ErrorType.Forbidden));
            }

            var specification =
                new EventBookingsSpecification(request);

            var pagedBookings = await _unitOfWork
                .GetRepository<Booking, Guid>()
                .GetPagedAsync(specification, cancellationToken);

            var customerIds = pagedBookings.Items
                .Select(booking => booking.UserId)
                .Distinct()
                .ToArray();

            var customers = await _customerInfoLookup.GetByUserIdsAsync(
                customerIds,
                cancellationToken);

            var bookingResponses = pagedBookings.Items
                .Select(booking =>
                {
                    customers.TryGetValue(
                        booking.UserId,
                        out var customer);

                    return new OrganizerBookingListResponse
                    {
                        BookingId = booking.Id,
                        UserId = booking.UserId,

                        CustomerName =
                            customer?.DisplayName ?? "Unknown customer",

                        CustomerEmail =
                            customer?.Email ?? string.Empty,

                        BookingStatus = booking.Status,
                        CreatedAt = booking.CreatedAt,
                        ExpiresAt = booking.ExpiresAt,
                        TotalAmount = booking.TotalAmount,

                        TicketQuantity = booking.BookingItems
                            .Sum(item => item.Quantity),

                        Items = booking.BookingItems
                            .Select(item => new CreateBookingItemResponse
                            {
                                TicketCategoryId = item.TicketCategoryId,

                                CategoryName = item.TicketCategory is not null
                                    ? item.TicketCategory.Name
                                    : "General Admission",

                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                TotalPrice = item.TotalPrice
                            })
                            .ToList()
                    };
                })
                .ToList();

            var response = new PagedResult<OrganizerBookingListResponse>(
                bookingResponses,
                pagedBookings.PageNumber,
                pagedBookings.PageSize,
                pagedBookings.TotalCount,
                pagedBookings.TotalPages);

            return Result<PagedResult<OrganizerBookingListResponse>>.Ok(
                response);
        }
    }
}