using EventHub.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace EventHub.Infrastructure.Redis
{
    public class CacheService<TEntity> : ICacheService<TEntity>
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _multiplexer;
        public CacheService(IDistributedCache cache, IConnectionMultiplexer multiplexer) 
        { 
           _cache = cache;
           _multiplexer = multiplexer;
        }
        public async Task AddCacheAsync(string key, TEntity entity, TimeSpan expiration, CancellationToken ct = default)
        {
            await _cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(entity), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            }, ct);
            
        }

        public async Task AddCacheAsync(string key, TEntity entity, CancellationToken ct = default)
        {
            await _cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(entity),ct);
        }

        public async Task DeleteCacheAsync(string key, CancellationToken ct = default)
        {
          await _cache.RemoveAsync(key, ct);
        }

        public async Task<TEntity?> GetCacheAsync(string key, CancellationToken ct = default)
        {
            var cachedData = await _cache.GetAsync(key, ct);
            if (cachedData is null)
            {
                return default;
            }
            var entity=JsonSerializer.Deserialize<TEntity>(cachedData);
            return entity;

        }

        public async Task<bool> TryAddCacheAsync(string key, TEntity entity, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var database = _multiplexer.GetDatabase();

            var serializedEntity = JsonSerializer.Serialize(entity);

            return await database.StringSetAsync(
                key,
                serializedEntity,
                when: When.NotExists);
        }
    }
}
