

namespace EventHub.Application.Common.Results
{
    public sealed class PagedResult<TEntity> 
    {
        public PagedResult(IReadOnlyList<TEntity> items, int pageNumber, int pageSize, int totalCount, int totalPages)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = totalPages;
        }

        public IReadOnlyList<TEntity> Items { get;}
        public int PageNumber { get;}
        public int PageSize { get;}
        public int TotalCount { get;}
        public int TotalPages { get;}

    }
}
