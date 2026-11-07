
namespace EventHub.Application.Contracts
{
    public interface IBookingExpirationService
    {
       Task ProcessingExpiredBookingsAsync(CancellationToken ct=default);
    }
}
