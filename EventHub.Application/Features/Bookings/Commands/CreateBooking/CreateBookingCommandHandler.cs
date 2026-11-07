using AutoMapper;
using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Bookings.Commands.CreateBooking
{
    public sealed class CreateBookingCommandHandler
        : IRequestHandler<CreateBookingCommand, Result<CreateBookingResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public CreateBookingCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<Result<CreateBookingResponse>> Handle(
            CreateBookingCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == Guid.Empty)
                return UserErrors.Unauthorized;

            var input = request.Request;
            var idempotencyKey = request.IdempotencyKey?.Trim();

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Booking.IdempotencyKeyRequired",
                        "The Idempotency-Key header is required.",
                        ErrorType.Validation));
            }

            if (idempotencyKey.Length > 100)
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Booking.IdempotencyKeyTooLong",
                        "The Idempotency-Key cannot exceed 100 characters.",
                        ErrorType.Validation));
            }

            if (input.Items is null || input.Items.Count == 0)
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Booking.ItemsEmpty",
                        "At least one ticket item is required.",
                        ErrorType.Validation));
            }

            if (input.Items.Any(item =>
                item.Quantity <= 0 ||
                item.TicketCategoryId == Guid.Empty))
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Booking.InvalidItems",
                        "Ticket category IDs, when provided, must be valid and quantities must be greater than zero.",
                        ErrorType.Validation));
            }

            var existingBooking = await GetBookingByIdempotencyKeyAsync(
                userId,
                idempotencyKey,
                cancellationToken);

            if (existingBooking is not null)
            {
                if (!HasSameRequest(existingBooking, input))
                {
                    return IdempotencyConflict();
                }

                var existingResponse = await MapExistingBookingResponseAsync(
                    existingBooking,
                    cancellationToken);

                return Result<CreateBookingResponse>.Ok(existingResponse);
            }

            var now = DateTime.UtcNow;

            var specification =
                new GetEventForBookingSpecification(input.EventId);

            var eventEntity = await _unitOfWork
                .GetRepository<Event, Guid>()
                .GetByIdAsync(specification, cancellationToken);

            if (eventEntity is null)
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Event.NotFound",
                        "Event was not found.",
                        ErrorType.NotFound));
            }

            if (eventEntity.Status != EventStatus.Published)
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Event.NotPublished",
                        "The event is not published.",
                        ErrorType.Conflict));
            }

            if (eventEntity.StartDate <= now ||
                eventEntity.EndDate <= now)
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Event.BookingClosed",
                        "Booking is no longer available for this event.",
                        ErrorType.Conflict));
            }

            decimal totalAmount = 0;
            var bookingItems = new List<BookingItem>();

            if (eventEntity.TicketMode == TicketMode.Categorized)
            {
                if (input.Items.Any(item =>
                    !item.TicketCategoryId.HasValue))
                {
                    return Result<CreateBookingResponse>.Fail(
                        new Error(
                            "Booking.CategoryRequired",
                            "A ticket category is required for categorized events.",
                            ErrorType.Validation));
                }

                if (input.Items
                    .GroupBy(item => item.TicketCategoryId)
                    .Any(group => group.Count() > 1))
                {
                    return Result<CreateBookingResponse>.Fail(
                        new Error(
                            "Booking.DuplicateCategory",
                            "Duplicate ticket categories are not allowed.",
                            ErrorType.Validation));
                }

                foreach (var item in input.Items)
                {
                    var category = eventEntity.TicketCategories
                        .FirstOrDefault(category =>
                            category.Id == item.TicketCategoryId!.Value);

                    if (category is null)
                    {
                        return Result<CreateBookingResponse>.Fail(
                            new Error(
                                "Booking.InvalidCategory",
                                "The selected ticket category does not belong to this event.",
                                ErrorType.Validation));
                    }

                    if (item.Quantity > category.AvailableQuantity)
                    {
                        return Result<CreateBookingResponse>.Fail(
                            new Error(
                                "Booking.InsufficientQuantity",
                                $"Not enough tickets are available for {category.Name}.",
                                ErrorType.Conflict));
                    }

                    var bookingItem = _mapper.Map<BookingItem>(item);

                    bookingItem.TicketCategoryId = category.Id;
                    bookingItem.TicketCategory = category;
                    bookingItem.Quantity = item.Quantity;
                    bookingItem.UnitPrice = category.Price;
                    bookingItem.TotalPrice =
                        category.Price * item.Quantity;

                    bookingItems.Add(bookingItem);
                    totalAmount += bookingItem.TotalPrice;
                }
            }
            else if (eventEntity.TicketMode == TicketMode.GeneralAdmission)
            {
                if (input.Items.Count != 1 ||
                    input.Items[0].TicketCategoryId.HasValue)
                {
                    return Result<CreateBookingResponse>.Fail(
                        new Error(
                            "Booking.InvalidGeneralAdmissionItems",
                            "General admission bookings require one item without a ticket category.",
                            ErrorType.Validation));
                }

                var item = input.Items[0];

                if (item.Quantity > eventEntity.AvailableQuantity)
                {
                    return Result<CreateBookingResponse>.Fail(
                        new Error(
                            "Booking.InsufficientQuantity",
                            "Not enough general admission tickets are available.",
                            ErrorType.Conflict));
                }

                var bookingItem = _mapper.Map<BookingItem>(item);

                bookingItem.TicketCategoryId = null;
                bookingItem.TicketCategory = null;
                bookingItem.Quantity = item.Quantity;
                bookingItem.UnitPrice = eventEntity.Price;
                bookingItem.TotalPrice =
                    eventEntity.Price * item.Quantity;

                bookingItems.Add(bookingItem);
                totalAmount += bookingItem.TotalPrice;
            }
            else
            {
                return Result<CreateBookingResponse>.Fail(
                    new Error(
                        "Booking.UnsupportedTicketMode",
                        "The event ticket mode is not supported.",
                        ErrorType.Conflict));
            }

            var booking = new Booking
            {
                UserId = userId,
                EventId = eventEntity.Id,
                Event = eventEntity,
                Status = BookingStatus.Pending,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                TotalAmount = totalAmount,
                BookingItems = bookingItems,
                IdempotencyKey = idempotencyKey
            };

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                if (eventEntity.TicketMode == TicketMode.Categorized)
                {
                    foreach (var item in input.Items)
                    {
                        var category = eventEntity.TicketCategories
                            .First(category =>
                                category.Id == item.TicketCategoryId!.Value);

                        category.AvailableQuantity -= item.Quantity;
                    }

                    eventEntity.AvailableQuantity = eventEntity.TicketCategories
                        .Sum(category => category.AvailableQuantity);
                }
                else
                {
                    eventEntity.AvailableQuantity -=
                        input.Items[0].Quantity;
                }

                _unitOfWork
                    .GetRepository<Booking, Guid>()
                    .Add(booking);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = _mapper.Map<CreateBookingResponse>(booking);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<CreateBookingResponse>.Ok(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);

                var concurrentlyCreatedBooking =
                    await GetBookingByIdempotencyKeyAsync(
                        userId,
                        idempotencyKey,
                        CancellationToken.None);

                if (concurrentlyCreatedBooking is not null)
                {
                    if (!HasSameRequest(concurrentlyCreatedBooking, input))
                    {
                        return IdempotencyConflict();
                    }

                    var existingResponse =
                        await MapExistingBookingResponseAsync(
                            concurrentlyCreatedBooking,
                            CancellationToken.None);

                    return Result<CreateBookingResponse>.Ok(existingResponse);
                }

                throw;
            }
        }

        private async Task<Booking?> GetBookingByIdempotencyKeyAsync(
            Guid userId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var specification =
                new GetBookingByIdempotencyKeySpecification(
                    userId,
                    idempotencyKey);

            return await _unitOfWork
                .GetRepository<Booking, Guid>()
                .GetByIdAsync(specification, cancellationToken);
        }

        private static bool HasSameRequest(
            Booking booking,
            CreateBookingRequest request)
        {
            if (booking.EventId != request.EventId ||
                request.Items is null ||
                booking.BookingItems.Count != request.Items.Count)
            {
                return false;
            }

            var existingItems = booking.BookingItems
                .Select(item => (
                    CategoryId: item.TicketCategoryId,
                    Quantity: item.Quantity))
                .OrderBy(item => item.CategoryId)
                .ThenBy(item => item.Quantity)
                .ToArray();

            var requestedItems = request.Items
                .Select(item => (
                    CategoryId: item.TicketCategoryId,
                    Quantity: item.Quantity))
                .OrderBy(item => item.CategoryId)
                .ThenBy(item => item.Quantity)
                .ToArray();

            return existingItems.SequenceEqual(requestedItems);
        }

        private async Task<CreateBookingResponse> MapExistingBookingResponseAsync(
            Booking booking,
            CancellationToken cancellationToken)
        {
            var eventSpecification =
                new GetEventForBookingSpecification(booking.EventId);

            var eventEntity = await _unitOfWork
                .GetRepository<Event, Guid>()
                .GetByIdAsync(eventSpecification, cancellationToken);

            if (eventEntity is not null)
            {
                booking.Event = eventEntity;

                foreach (var item in booking.BookingItems)
                {
                    if (item.TicketCategoryId.HasValue)
                    {
                        item.TicketCategory =
                            eventEntity.TicketCategories.FirstOrDefault(
                                category =>
                                    category.Id == item.TicketCategoryId.Value);
                    }
                }
            }

            return _mapper.Map<CreateBookingResponse>(booking);
        }

        private static Result<CreateBookingResponse> IdempotencyConflict()
        {
            return Result<CreateBookingResponse>.Fail(
                new Error(
                    "Booking.IdempotencyKeyConflict",
                    "This Idempotency-Key has already been used for a different booking request.",
                    ErrorType.Conflict));
        }
    }
}
