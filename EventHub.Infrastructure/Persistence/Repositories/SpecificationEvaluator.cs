using EventHub.Application.Contracts;
using EventHub.Domain.Common;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace EventHub.Infrastructure.Persistence.Repositories
{
    public class SpecificationEvaluator<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public static IQueryable<TEntity> Evaluate(IQueryable<TEntity> query,ISpecification<TEntity, TKey> specification)
        {
            if(specification.Criteria!=null)
            {
                query=query.Where(specification.Criteria);
            }
         
            foreach(var include in specification.Includes)
            {
                    query=query.Include(include);
            }

            foreach (var includePath in specification.IncludePaths)
            {
                query = query.Include(includePath);
            }

            if (specification.OrderExpressions.Any())
            {
                IOrderedQueryable<TEntity>? orderedQuery = null;

                foreach (var order in specification.OrderExpressions)
                {
                    if (orderedQuery is null)
                    {
                        orderedQuery = order.Direction == SortDirection.Ascending
                            ? query.OrderBy(order.Expression)
                            : query.OrderByDescending(order.Expression);
                    }
                    else
                    {
                        orderedQuery = order.Direction == SortDirection.Ascending
                            ? orderedQuery.ThenBy(order.Expression)
                            : orderedQuery.ThenByDescending(order.Expression);
                    }
                }

                if (orderedQuery is not null)
                {
                    query = orderedQuery;
                }
            }


            if (specification.IsPaginated)
            {
                query=query.Skip(specification.Skip).Take(specification.Take);
            }
            return query;
        } 
    }
    
}
