using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Bookings.Queries.GetEventBookingSummary
{
    public sealed class GetEventBookingSummaryQueryHandler:IRequestHandler<GetEventBookingSummaryQuery,Result<EventBookingSummaryResponse>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBookingSummaryReader _summaryReader;

        public GetEventBookingSummaryQueryHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork,
            IEventBookingSummaryReader summaryReader)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _summaryReader = summaryReader;
        }

        public async Task<Result<EventBookingSummaryResponse>> Handle(GetEventBookingSummaryQuery request,CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == Guid.Empty)
            {
                return UserErrors.Unauthorized;
            }

            var eventSpecification = new GetEventForBookingSpecification(request.EventId);

            var eventEntity = await _unitOfWork.GetRepository<Event, Guid>()
                .GetByIdAsync(
                    eventSpecification,
                    cancellationToken);

            if (eventEntity is null)
            {
                return Result<EventBookingSummaryResponse>.Fail(
                    new Error(
                        "Event.NotFound",
                        "Event was not found.",
                        ErrorType.NotFound));
            }

            if (eventEntity.OrganizerId != userId)
            {
                return Result<EventBookingSummaryResponse>.Fail(
                    new Error(
                        "Event.Forbidden",
                        "You are not allowed to view booking summary for this event.",
                        ErrorType.Forbidden));
            }

            var utcNow = DateTime.UtcNow;

            var stats = await _summaryReader.GetStatsAsync(
                eventEntity.Id,
                utcNow,
                cancellationToken);

            var capacity = eventEntity.Capacity;
            var availableQuantity = eventEntity.AvailableQuantity;

            if (eventEntity.TicketMode == TicketMode.Categorized)
            {
                capacity = eventEntity.TicketCategories
                    .Sum(category => category.Capacity);

                availableQuantity = eventEntity.TicketCategories
                    .Sum(category => category.AvailableQuantity);
            }

            var response = new EventBookingSummaryResponse
            {
                EventId = eventEntity.Id,
                EventName = eventEntity.Name,
                EventStatus = eventEntity.Status,
                TicketMode = eventEntity.TicketMode,

                Capacity = capacity,
                AvailableQuantity = availableQuantity,

                SoldQuantity = stats.SoldQuantity,
                HeldQuantity = stats.HeldQuantity,

                TotalBookings = stats.TotalBookings,
                PendingBookings = stats.PendingBookings,
                ConfirmedBookings = stats.ConfirmedBookings,
                ExpiredBookings = stats.ExpiredBookings,
                CancelledBookings = stats.CancelledBookings
            };

            return Result<EventBookingSummaryResponse>.Ok(response);
        }
    }
}