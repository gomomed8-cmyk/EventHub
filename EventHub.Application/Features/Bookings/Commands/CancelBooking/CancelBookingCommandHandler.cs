using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Bookings.Commands.CancelBooking
{
    public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        public CancelBookingCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Unit>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (userId == Guid.Empty)
            {
                return Result<Unit>.Fail(
                    new Error(
                        "User is not authenticated",
                        "User.Unauthorized",
                        ErrorType.Unauthorized));
            }

            var bookingRepository = _unitOfWork.GetRepository<Booking, Guid>();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var spec = new CancelBookingSpecification(
                    request.BookingId,
                    userId);

                var booking = await bookingRepository.GetByIdAsync(
                    spec,
                    cancellationToken);

                if (booking is null)
                {
                    await _unitOfWork.RollbackTransactionAsync(
                        CancellationToken.None);

                    return Result<Unit>.Fail(
                        new Error(
                            "Booking not found",
                            "Booking.NotFound",
                            ErrorType.NotFound));
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    var error = booking.Status switch
                    {
                        BookingStatus.Expired => new Error(
                            "Expired bookings cannot be cancelled",
                            "Booking.InvalidStatus",
                            ErrorType.Conflict),

                        BookingStatus.Confirmed => new Error(
                            "Confirmed bookings cannot be cancelled",
                            "Booking.InvalidStatus",
                            ErrorType.Conflict),

                        BookingStatus.Cancelled => new Error(
                            "Booking is already cancelled",
                            "Booking.AlreadyCancelled",
                            ErrorType.Conflict),

                        _ => new Error(
                            "Booking cannot be cancelled in its current status",
                            "Booking.InvalidStatus",
                            ErrorType.Conflict)
                    };

                    await _unitOfWork.RollbackTransactionAsync(
                        CancellationToken.None);

                    return Result<Unit>.Fail(error);
                }

                foreach (var item in booking.BookingItems)
                {
                    if (item.TicketCategoryId is null)
                    {
                        booking.Event.AvailableQuantity += item.Quantity;
                    }
                    else
                    {
                        var category = booking.Event.TicketCategories
                            .SingleOrDefault(
                                c => c.Id == item.TicketCategoryId.Value);

                        if (category is null)
                        {
                            throw new InvalidOperationException(
                                "The booking ticket category was not found.");
                        }

                        category.AvailableQuantity += item.Quantity;
                    }
                }

                booking.Status = BookingStatus.Cancelled;

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<Unit>.Ok(Unit.Value);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(
                    CancellationToken.None);

                throw;
            }
        }
    }
}
