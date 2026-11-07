using EventHub.Application.DTOs.Booking;

namespace EventHub.Application.Contracts
{
    public interface IEventBookingSummaryReader
    {
        Task<EventBookingSummaryStats> GetStatsAsync(
            Guid eventId,
            DateTime utcNow,
            CancellationToken cancellationToken = default);
    }
}