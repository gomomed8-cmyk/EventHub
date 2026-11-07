using EventHub.Domain.Enums;
using System.Linq.Expressions;


namespace EventHub.Application.Specifications.Events
{
    public sealed record SpecificationOrder<TEntity>(
        Expression<Func<TEntity, object>> Expression,
        SortDirection Direction);
}
