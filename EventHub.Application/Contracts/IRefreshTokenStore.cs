

namespace EventHub.Application.Contracts
{
    
        public interface IRefreshTokenStore
        {
            Task<bool> StoreRefreshTokenAsync(
                string userId,
                string refreshToken,
                TimeSpan expiration,
                CancellationToken ct = default);

            Task<string?> GetRefreshTokenAsync(
                string userId,
                CancellationToken ct = default);

               Task<string?> GetUserIdByRefreshTokenAsync(
                      string refreshToken,
                  CancellationToken ct = default);

            Task DeleteRefreshTokenAsync(
                string userId,
                string refreshToken,
                CancellationToken ct = default);
    }
    
}
