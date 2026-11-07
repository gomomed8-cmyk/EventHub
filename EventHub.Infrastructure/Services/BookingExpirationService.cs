using EventHub.Application.Contracts;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Services
{
    public sealed class BookingExpirationService
        : IBookingExpirationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingExpirationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessingExpiredBookingsAsync(
            CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var bookingRepository =
                _unitOfWork.GetRepository<Booking, Guid>();

            var categoryRepository =
                _unitOfWork.GetRepository<TicketCategory, Guid>();

            var eventRepository =
                _unitOfWork.GetRepository<Event, Guid>();

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                var specification =
                    new ExpiredBookingsSpecification(now);

                var bookings = await bookingRepository.GetAllAsync(
                    specification,
                    ct);

                if (bookings.Count == 0)
                {
                    await _unitOfWork.CommitTransactionAsync(ct);
                    return;
                }

                foreach (var booking in bookings)
                {
                    if (booking.Status != BookingStatus.Pending ||
                        !booking.ExpiresAt.HasValue ||
                        booking.ExpiresAt.Value > now)
                    {
                        continue;
                    }

                    if (booking.Event is null)
                    {
                        throw new InvalidOperationException(
                            "The event associated with the booking was not found.");
                    }

                    foreach (var item in booking.BookingItems)
                    {
                        if (item.Quantity <= 0)
                        {
                            throw new InvalidOperationException(
                                "Booking item quantity must be greater than zero.");
                        }

                        if (booking.Event.TicketMode ==
                            TicketMode.Categorized)
                        {
                            if (!item.TicketCategoryId.HasValue)
                            {
                                throw new InvalidOperationException(
                                    "A categorized booking item must have a ticket category.");
                            }

                            var category =
                                await categoryRepository.GetByIdAsync(
                                    item.TicketCategoryId.Value,
                                    ct);

                            if (category is null ||
                                category.EventId != booking.EventId)
                            {
                                throw new InvalidOperationException(
                                    "The ticket category does not belong to this event.");
                            }

                            category.AvailableQuantity = checked(
                                category.AvailableQuantity + item.Quantity);
                        }
                        else if (booking.Event.TicketMode ==
                                 TicketMode.GeneralAdmission)
                        {
                            if (item.TicketCategoryId.HasValue)
                            {
                                throw new InvalidOperationException(
                                    "General Admission booking items must not have a ticket category.");
                            }

                            booking.Event.AvailableQuantity = checked(
                                booking.Event.AvailableQuantity + item.Quantity);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                "The event has an unsupported ticket mode.");
                        }
                    }

                    booking.Status = BookingStatus.Expired;
                }

                await _unitOfWork.SaveChangesAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);

                
                throw;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }
    }
}
