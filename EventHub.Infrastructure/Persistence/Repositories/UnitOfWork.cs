using EventHub.Application.Contracts;
using EventHub.Domain.Common;
using EventHub.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventHub.Infrastructure.Persistence.Repositories
{
    internal class UnitOfWork(HubDbContext _context) : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = [];
        private IDbContextTransaction? _transaction;

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException(
                    "A transaction is already active.");
            }

            _transaction = await _context.Database
                .BeginTransactionAsync(ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException(
                    "No active transaction to commit.");
            }

            try
            {
                await _transaction.CommitAsync(ct);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_transaction == null)
            {
                return;
            }

            try
            {
                await _transaction.RollbackAsync(ct);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
            where TEntity : BaseEntity<TKey>
        {
            var entityType = typeof(TEntity);

            if (_repositories.TryGetValue(entityType, out object? value))
                return (IGenericRepository<TEntity, TKey>)value;

            var repo = new GenericRepository<TEntity, TKey>(_context);
            _repositories[entityType] = repo;
            return repo;
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }
    }
}
