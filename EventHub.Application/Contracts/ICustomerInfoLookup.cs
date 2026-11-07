namespace EventHub.Application.Contracts
{
    public sealed record CustomerInfo(
        Guid UserId,
        string? DisplayName,
        string? Email);

    public interface ICustomerInfoLookup
    {
        Task<IReadOnlyDictionary<Guid, CustomerInfo>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken ct = default);
    }
}