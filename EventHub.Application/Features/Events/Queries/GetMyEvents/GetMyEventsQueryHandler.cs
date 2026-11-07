using EventHub.Application.Common.Errors;
using EventHub.Application.Common.Results;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Events.Queries.GetMyEvents
{
    public sealed class GetMyEventsQueryHandler : IRequestHandler<GetMyEventsQuery, Result<PagedResult<EventListResponse>>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        public GetMyEventsQueryHandler(ICurrentUserService currentUserService,IUnitOfWork unitOfWork)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<EventListResponse>>> Handle(GetMyEventsQuery request, CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId==Guid.Empty)
            {
                return Result<PagedResult<EventListResponse>>.Fail(
                    new Error(
                        "User.NotAuthenticated",
                        "User is not authenticated.",
                        ErrorType.Unauthorized));
            }

            var spec = new OrganizerEventsSpecification(_currentUserService.UserId,request);
          
            var myEvents = await _unitOfWork.GetRepository<Event, Guid>().GetPagedAsync(spec, cancellationToken);
            foreach (var item in myEvents.Items)
            {
                if(item.TicketMode==TicketMode.Categorized&&!item.TicketCategories.Any())
                {
                    return Result<PagedResult<EventListResponse>>.Fail(
                       new Error(
                           "Event.InvalidTicketCategories",
                           $"Categorized event with Id {item.Id} has no ticket categories.",
                           ErrorType.Conflict));
                }
            }

            var eventResponses = myEvents.Items.Select(e => new EventListResponse
            {
                EventId = e.Id,
                Name = e.Name,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Category = e.Category,
                TicketMode = e.TicketMode,
                Price = e.TicketMode == TicketMode.GeneralAdmission ? e.Price : null,
                MinTicketPrice = e.TicketMode == TicketMode.Categorized ? e.TicketCategories.Min(tc => tc.Price) : null,
                MaxTicketPrice = e.TicketMode == TicketMode.Categorized ? e.TicketCategories.Max(tc => tc.Price) : null,
                AvailableQuantity = e.Capacity
            }).ToList();
            var pagedResponse = new PagedResult<EventListResponse>(
         eventResponses,
         myEvents.PageNumber,
         myEvents.PageSize,
         myEvents.TotalCount,
         myEvents.TotalPages);

            return Result<PagedResult<EventListResponse>>.Ok(pagedResponse);
        }
    }
}
