using EventHub.Application.Specifications.Events;
using EventHub.Domain.Common;

using System.Linq.Expressions;


namespace EventHub.Application.Contracts
{
    public interface ISpecification<TEntity,TKey> where TEntity : BaseEntity<TKey>
    {
        ICollection<Expression<Func<TEntity,object>>> Includes { get; }
        ICollection<string> IncludePaths { get; }
        Expression<Func<TEntity,bool>>? Criteria { get; }
        
         ICollection<SpecificationOrder<TEntity>> OrderExpressions {  get; }
        int Take { get; }
        int Skip { get; }
        int PageNumber { get; }
        int PageSize { get; }
        bool IsPaginated { get; }
    }
}
