

namespace EventHub.Application.Contracts
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }
}
