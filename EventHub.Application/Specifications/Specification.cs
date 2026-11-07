using EventHub.Application.Contracts;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Common;
using System.Linq.Expressions;

namespace  EventHub.Application.Specifications
{
    public class Specification<TEntity, TKey>: ISpecification<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public ICollection<Expression<Func<TEntity, object>>> Includes { get; }
            = new List<Expression<Func<TEntity, object>>>();

        public Expression<Func<TEntity, bool>>? Criteria { get; private set; }

       

        public int Take { get; private set; }

        public int Skip { get; private set; }
        public int PageNumber { get; private set; } = 1;
        public int PageSize { get; private set; } = 10;

        public bool IsPaginated { get; private set; }



        public ICollection<SpecificationOrder<TEntity>> OrderExpressions { get; private set; }
          = new List<SpecificationOrder<TEntity>>();

        public ICollection<string> IncludePaths { get; } = new List<string>();

        public Specification()
        {
        }

        public Specification(Expression<Func<TEntity, bool>> criteria)
        {
            Criteria = criteria;
        }

        public void AddInclude(Expression<Func<TEntity, object>> include)
        {
            Includes.Add(include);
        }

        public void AddIncludePath(string includePath)
        {
            if (string.IsNullOrWhiteSpace(includePath))
                throw new ArgumentException(
                    "Include path cannot be null or empty.",
                    nameof(includePath));

            IncludePaths.Add(includePath);
        }

        public void AddOrderExpressions(SpecificationOrder<TEntity> orders)
        {
            OrderExpressions.Add(orders);
        }

        public void ApplyPaging(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;

            Skip = (pageNumber - 1) * pageSize;
            Take = pageSize;

            IsPaginated = true;
        }
    }
}