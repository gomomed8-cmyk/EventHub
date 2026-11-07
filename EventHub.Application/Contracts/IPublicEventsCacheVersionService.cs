

namespace EventHub.Application.Contracts
{
    public interface IPublicEventsCacheVersionService
    {
        Task<string> GetOrCreateVersionAsync();

        Task RefreshVersionAsync();
    }
}
