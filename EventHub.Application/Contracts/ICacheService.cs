


namespace EventHub.Application.Contracts
{
    public interface ICacheService<TEntity> 
    {
        public Task<TEntity?> GetCacheAsync( string key,CancellationToken ct=default);
        public Task AddCacheAsync(string key,TEntity entity, TimeSpan expiration, CancellationToken ct=default);
        public Task DeleteCacheAsync( string key,CancellationToken ct=default);
        public Task AddCacheAsync(string key, TEntity entity, CancellationToken ct = default);
        public Task<bool> TryAddCacheAsync(string key,TEntity entity,CancellationToken ct=default);
    }
}
