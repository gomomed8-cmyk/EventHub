using EventHub.Application.Common.Results;
using EventHub.Domain.Common;


namespace EventHub.Application.Contracts
{
    public interface IGenericRepository<TEntity,TKey> where TEntity : BaseEntity<TKey>
    {
        void Add(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task<TEntity?> GetByIdAsync(TKey id,CancellationToken ct=default);
        Task<TEntity?> GetByIdAsync(ISpecification<TEntity,TKey> spec,CancellationToken ct=default);
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct=default);
        Task<IReadOnlyList<TEntity>> GetAllAsync(ISpecification<TEntity,TKey> spec, CancellationToken ct=default);
        Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity, TKey> spec, CancellationToken ct = default);
        Task<int> CountAsync(ISpecification<TEntity, TKey> spec,CancellationToken ct = default);
    }
}
