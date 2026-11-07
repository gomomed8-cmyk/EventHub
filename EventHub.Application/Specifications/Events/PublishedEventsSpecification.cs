using EventHub.Application.Features.Events.Queries.GetAllEvent;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;


namespace EventHub.Application.Specifications.Events
{
    public class PublishedEventsSpecification:Specification<Event,Guid>
    {
        public PublishedEventsSpecification(GetAllEventsQuery query)
            : base(e => e.Status == EventStatus.Published
        && (string.IsNullOrWhiteSpace(query.SearchTerm) || e.Name.Contains(query.SearchTerm) || e.Description.Contains(query.SearchTerm))
            &&(!query.Category.HasValue ||e.Category==query.Category.Value)
            &&(!query.StartDateFrom.HasValue ||e.StartDate>=query.StartDateFrom)
            &&(!query.StartDateTo.HasValue || e.StartDate<=query.StartDateTo))
        { 
          AddInclude(e => e.TicketCategories);

            var sortBy = query.SortBy?.Trim().ToLowerInvariant();

            var direction = query.SortDescending
                ? SortDirection.Descending
                : SortDirection.Ascending;

            switch (sortBy)
            {
                case "name":
                    AddOrderExpressions(
                        new SpecificationOrder<Event>(
                            e => e.Name,
                            direction));
                    break;

                case "price":
                    if (query.SortDescending)
                    {
                        AddOrderExpressions(
                            new SpecificationOrder<Event>(
                                e => e.TicketMode == TicketMode.GeneralAdmission
                                    ? e.Price
                                    : e.TicketCategories.Min(tc => tc.Price),
                                SortDirection.Descending));
                    }
                    else
                    {
                        AddOrderExpressions(
                            new SpecificationOrder<Event>(
                                e => e.TicketMode == TicketMode.GeneralAdmission
                                    ? e.Price
                                    : e.TicketCategories.Min(tc => tc.Price),
                                SortDirection.Ascending));
                    }
                    break;

                case "startdate":
                default:
                    AddOrderExpressions(
                        new SpecificationOrder<Event>(
                            e => e.StartDate,
                            direction));
                    break;
            }

            AddOrderExpressions(
                new SpecificationOrder<Event>(
                    e => e.Id,
                    SortDirection.Ascending));



            var page = Math.Max(query.PageNumber ?? 1, 1);
            var size = Math.Clamp(query.PageSize ?? 10, 1, 100);

            ApplyPaging(page, size);
        }
    }
}
