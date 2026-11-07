using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Auth;


namespace EventHub.Application.Contracts
{
    public interface IIdentityService
    {
        Task<Result<Guid>> CreateUserAsync( RegisterRequest request,CancellationToken ct=default);
        Task<Result<LoginUserInfo>> LoginAsync(string email,string password,CancellationToken ct=default);
        Task<Result<LoginUserInfo>> GetUserByIdAsync(Guid userId,CancellationToken ct = default);
        Task<Result> PromoteToOrganizerAsync(Guid userId,CancellationToken ct = default);
        Task<Result<UserEmailInfo>> GetUserEmailInfoAsync(Guid userId,CancellationToken ct = default);
    }
}
    