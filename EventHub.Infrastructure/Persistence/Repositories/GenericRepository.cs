using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using EventHub.Domain.Common;
using EventHub.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;


namespace EventHub.Infrastructure.Persistence.Repositories
{
    internal class GenericRepository<TEntity, TKey>(HubDbContext dbContext) : IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public void Add(TEntity entity)=>dbContext.Set<TEntity>().Add(entity);

        public void Delete(TEntity entity)=>dbContext.Set<TEntity>().Remove(entity);

        public void Update(TEntity entity)=>dbContext.Set<TEntity>().Update(entity);
        

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)=>await dbContext.Set<TEntity>().ToListAsync(ct);


        public async Task<TEntity?> GetByIdAsync( TKey id,CancellationToken ct = default)
        {
            return await dbContext.Set<TEntity>()
                .FindAsync(new object?[] { id }, ct);
        }

        public async Task<TEntity?> GetByIdAsync(ISpecification<TEntity, TKey> spec, CancellationToken ct = default)
        {
          IQueryable<TEntity> query = dbContext.Set<TEntity>();
           query = SpecificationEvaluator<TEntity, TKey>.Evaluate(query, spec);
              return await query.FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(ISpecification<TEntity, TKey> spec, CancellationToken ct = default)
        {
          IQueryable<TEntity> query = dbContext.Set<TEntity>();
           query = SpecificationEvaluator<TEntity, TKey>.Evaluate(query, spec);
              return await query.ToListAsync(ct);
        }

        public async Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity, TKey> spec, CancellationToken ct = default)
        {

            IQueryable<TEntity> query = dbContext.Set<TEntity>();
            int totalCount = await CountAsync(spec, ct);
            query=SpecificationEvaluator<TEntity,TKey>.Evaluate(query, spec);
            var item= await query.ToListAsync(ct);
            int totalPages = (int)Math.Ceiling((double)totalCount / spec.PageSize);
            return new PagedResult<TEntity>(item, spec.PageNumber, spec.PageSize, totalCount, totalPages);
        }

        public async Task<int> CountAsync(ISpecification<TEntity, TKey> spec, CancellationToken ct = default)
        {
          IQueryable<TEntity> query= dbContext.Set<TEntity>();
            if(spec.Criteria is not null)
            {
                query=query.Where(spec.Criteria);
            }
            return await query.CountAsync(ct);
            
        }
    }
}
