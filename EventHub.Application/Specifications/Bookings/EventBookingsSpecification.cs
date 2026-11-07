using EventHub.Application.Features.Bookings.Queries.GetEventBookings;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;

namespace EventHub.Application.Specifications.Bookings
{
    public sealed class EventBookingsSpecification
        : Specification<Booking, Guid>
    {
        public EventBookingsSpecification(GetEventBookingsQuery query)
            : base(b =>
                b.EventId == query.EventId &&
                (!query.Status.HasValue ||
                    b.Status == query.Status.Value) &&
                (!query.CreatedFrom.HasValue ||
                    b.CreatedAt >= query.CreatedFrom.Value) &&
                (!query.CreatedTo.HasValue ||
                    b.CreatedAt <= query.CreatedTo.Value))
        {

            AddInclude(b => b.Event.TicketCategories);
            AddInclude(b => b.BookingItems);

            var direction = query.SortDescending
                ? SortDirection.Descending
                : SortDirection.Ascending;

            var sortBy = query.SortBy?.Trim().ToLowerInvariant();

            switch (sortBy)
            {
                case "totalamount":
                    AddOrderExpressions(
                        new SpecificationOrder<Booking>(
                            b => b.TotalAmount,
                            direction));
                    break;

                case "status":
                    AddOrderExpressions(
                        new SpecificationOrder<Booking>(
                            b => b.Status,
                            direction));
                    break;

                case "expiresat":
                    AddOrderExpressions(
                        new SpecificationOrder<Booking>(
                            b => b.ExpiresAt,
                            direction));
                    break;

                case "createdat":
                default:
                    AddOrderExpressions(
                        new SpecificationOrder<Booking>(
                            b => b.CreatedAt,
                            direction));
                    break;
            }

            AddOrderExpressions(
                new SpecificationOrder<Booking>(
                    b => b.Id,
                    SortDirection.Ascending));

            var page = Math.Max(query.PageNumber ?? 1, 1);
            var size = Math.Clamp(query.PageSize ?? 10, 1, 100);

            ApplyPaging(page, size);
        }
    }
}