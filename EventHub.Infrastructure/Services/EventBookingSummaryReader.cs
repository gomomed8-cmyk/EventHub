using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Booking;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Services
{
    public sealed class EventBookingSummaryReader:IEventBookingSummaryReader
    {
        private readonly HubDbContext _dbContext;

        public EventBookingSummaryReader(HubDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EventBookingSummaryStats> GetStatsAsync(
            Guid eventId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var bookingCounts = await _dbContext.Set<Booking>()
                .AsNoTracking()
                .Where(b => b.EventId == eventId)
                .GroupBy(b => b.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            var countsByStatus = bookingCounts.ToDictionary(
                item => item.Status,
                item => item.Count);

            int GetCount(BookingStatus status)
            {
                return countsByStatus.TryGetValue(
                    status,
                    out var count)
                    ? count
                    : 0;
            }

            var quantityTotals = await _dbContext
                .Set<BookingItem>()
                .AsNoTracking()
                .Where(item =>
                    item.Booking.EventId == eventId &&
                    (
                        item.Booking.Status == BookingStatus.Confirmed
                        ||
                        (
                            item.Booking.Status == BookingStatus.Pending
                            && item.Booking.ExpiresAt.HasValue
                            && item.Booking.ExpiresAt.Value > utcNow
                        )
                    ))
                .GroupBy(item => item.Booking.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .ToListAsync(cancellationToken);

            var soldQuantity = quantityTotals
                .Where(item =>
                    item.Status == BookingStatus.Confirmed)
                .Select(item => item.Quantity)
                .FirstOrDefault();

            var heldQuantity = quantityTotals
                .Where(item =>
                    item.Status == BookingStatus.Pending)
                .Select(item => item.Quantity)
                .FirstOrDefault();

            return new EventBookingSummaryStats
            {
                TotalBookings = bookingCounts.Sum(item => item.Count),

                PendingBookings = GetCount(BookingStatus.Pending),

                ConfirmedBookings = GetCount(BookingStatus.Confirmed),

                ExpiredBookings = GetCount(BookingStatus.Expired),

                CancelledBookings = GetCount(BookingStatus.Cancelled),

                SoldQuantity = soldQuantity,

                HeldQuantity = heldQuantity
            };
        }
    }
}