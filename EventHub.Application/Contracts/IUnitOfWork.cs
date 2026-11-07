using EventHub.Domain.Common;


namespace EventHub.Application.Contracts
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct=default);
        IGenericRepository<TEntity,TKey> GetRepository<TEntity,TKey>() where TEntity : BaseEntity<TKey>;
        Task BeginTransactionAsync(CancellationToken ct=default);
        Task CommitTransactionAsync(CancellationToken ct= default);
        Task RollbackTransactionAsync(CancellationToken ct=default);
    }
}
