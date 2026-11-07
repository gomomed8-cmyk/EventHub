

namespace EventHub.Application.Contracts
{
    public interface IEventMaintenanceService
    {
        Task CompleteExpiredEventsAsync(CancellationToken ct = default);
    }
}
