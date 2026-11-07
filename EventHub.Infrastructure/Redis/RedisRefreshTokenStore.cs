using EventHub.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;


namespace EventHub.Infrastructure.Redis
{
    internal class RedisRefreshTokenStore : IRefreshTokenStore
    {
        private readonly IDistributedCache _cache;
        public RedisRefreshTokenStore(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task DeleteRefreshTokenAsync(string userId, string refreshToken, CancellationToken ct = default)
        {
           await _cache.RemoveAsync(userId,ct);
           await _cache.RemoveAsync(refreshToken,ct);

        }
        public async Task<string?> GetRefreshTokenAsync(string userId, CancellationToken ct = default)
        {
            var token = await _cache.GetAsync(userId,ct);
            if (token == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(token);
        }

        public async Task<string?> GetUserIdByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        {
           var result=await _cache.GetAsync(refreshToken,ct);
            if (result==null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(result);
        }

        public async Task<bool> StoreRefreshTokenAsync(string userId, string refreshToken, TimeSpan expiration, CancellationToken ct = default)
        {
            await _cache.SetAsync(userId, Encoding.UTF8.GetBytes(refreshToken), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
            await _cache.SetAsync(refreshToken, Encoding.UTF8.GetBytes(userId), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            }); 
           return true;
        }
    }
}
