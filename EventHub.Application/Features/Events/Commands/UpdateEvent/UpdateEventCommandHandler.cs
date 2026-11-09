using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Events.Commands.UpdateEvent
{
    public sealed class UpdateEventCommandHandler
        : IRequestHandler<UpdateEventCommand, Result<EventResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublicEventsCacheVersionService _cacheVersionService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateEventCommandHandler(IUnitOfWork unitOfWork,IPublicEventsCacheVersionService cacheVersionService,ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _cacheVersionService = cacheVersionService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<EventResponse>> Handle(
            UpdateEventCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            
            var spec=new UpdateEventSpecification(request.EventId);

            var eventRepository = _unitOfWork.GetRepository<Event, Guid>();

            var eventEntity = await eventRepository.GetByIdAsync(spec, cancellationToken);

            if (eventEntity == null)
            {
                return Result<EventResponse>.Fail(
                    new Error(
                        "Event.NotFound",
                        "Event was not found.",
                        ErrorType.NotFound));
            }

            if (!_currentUserService.IsInRole("Admin") && eventEntity.OrganizerId != currentUserId)
            {
                return Result<EventResponse>.Fail(
                    new Error(
                        "It is Not Allowed to Update this event",
                        "event.Forbidden",
                        ErrorType.Forbidden));
            }

            if (eventEntity.Status != EventStatus.Draft)
            {
                return Result<EventResponse>.Fail(
                    new Error(
                        "Event.UpdateForbidden",
                        "Event cannot be updated in its current status.",
                        ErrorType.Conflict));
            }

           
            var finalName =
                request.UpdateEventRequest.Name ?? eventEntity.Name;

            var finalDescription =
                request.UpdateEventRequest.Description ?? eventEntity.Description;

            var finalStartDate =
                request.UpdateEventRequest.StartDate ?? eventEntity.StartDate;

            var finalEndDate =
                request.UpdateEventRequest.EndDate ?? eventEntity.EndDate;

            var finalTicketMode =
                request.UpdateEventRequest.TicketMode ?? eventEntity.TicketMode;
            var finalCatogry=request.UpdateEventRequest.Category ?? eventEntity.Category;

            
            if (finalStartDate >= finalEndDate)
            {
                return Result<EventResponse>.Fail(
                    new Error(
                        "Event.InvalidDateRange",
                        "Event start date must be before end date.",
                        ErrorType.Validation));
            }

            if (finalStartDate < DateTime.UtcNow)
            {
                return Result<EventResponse>.Fail(
                    new Error(
                        "Event.InvalidStartDate",
                        "Event start date cannot be in the past.",
                        ErrorType.Validation));
            }

           
            if (finalTicketMode == TicketMode.GeneralAdmission)
            {
                
                if (eventEntity.TicketMode != TicketMode.GeneralAdmission)
                {
                    if (request.UpdateEventRequest.Price is null ||
                        request.UpdateEventRequest.Capacity is null)
                    {
                        return Result<EventResponse>.Fail(
                            new Error(
                                "Event.GeneralAdmissionDataRequired",
                                "Price and capacity are required when changing to general admission.",
                                ErrorType.Validation));
                    }
                }

                var finalPrice =
                    request.UpdateEventRequest.Price ?? eventEntity.Price;

                var finalCapacity =
                    request.UpdateEventRequest.Capacity ?? eventEntity.Capacity;

                if (finalPrice < 0)
                {
                    return Result<EventResponse>.Fail(
                        new Error(
                            "Event.InvalidPrice",
                            "Event price must be non-negative.",
                            ErrorType.Validation));
                }

                if (finalCapacity <= 0)
                {
                    return Result<EventResponse>.Fail(
                        new Error(
                            "Event.InvalidCapacity",
                            "Event capacity must be greater than 0.",
                            ErrorType.Validation));
                }

                
                eventEntity.TicketCategories.Clear();

                eventEntity.Price = finalPrice;
                eventEntity.Capacity = finalCapacity;
            }

           
            else if (finalTicketMode == TicketMode.Categorized)
            {
                IReadOnlyList<TicketCategoryRequest>? finalCategories;

                if (request.UpdateEventRequest.TicketCategories is not null)
                {
                    finalCategories =
                        request.UpdateEventRequest.TicketCategories;
                }
                else
                {
                    finalCategories = eventEntity.TicketCategories
                        .Select(tc => new TicketCategoryRequest
                        {
                            Name = tc.Name,
                            Price = tc.Price,
                            Capacity = tc.Capacity
                        })
                        .ToList();
                }

                if (finalCategories.Count == 0)
                {
                    return Result<EventResponse>.Fail(
                        new Error(
                            "Event.NoTicketCategories",
                            "Categorized events must have at least one ticket category.",
                            ErrorType.Validation));
                }

                foreach (var category in finalCategories)
                {
                    if (string.IsNullOrWhiteSpace(category.Name))
                    {
                        return Result<EventResponse>.Fail(
                            new Error(
                                "TicketCategory.InvalidName",
                                "Ticket category name is required.",
                                ErrorType.Validation));
                    }

                    if (category.Capacity <= 0)
                    {
                        return Result<EventResponse>.Fail(
                            new Error(
                                "TicketCategory.InvalidCapacity",
                                "Ticket category capacity must be greater than 0.",
                                ErrorType.Validation));
                    }

                    if (category.Price < 0)
                    {
                        return Result<EventResponse>.Fail(
                            new Error(
                                "TicketCategory.InvalidPrice",
                                "Ticket category price must be non-negative.",
                                ErrorType.Validation));
                    }
                }

                
                eventEntity.TicketCategories.Clear();

                foreach (var category in finalCategories)
                {
                    eventEntity.TicketCategories.Add(
                        new TicketCategory
                        {
                            Name = category.Name,
                            Price = category.Price,
                            Capacity = category.Capacity
                        });
                }

               
                eventEntity.Price = 0;

                
                eventEntity.Capacity =
                    finalCategories.Sum(category => category.Capacity);
            }

           
            eventEntity.Name = finalName;
            eventEntity.Description = finalDescription;
            eventEntity.StartDate = finalStartDate;
            eventEntity.EndDate = finalEndDate;
            eventEntity.Category = finalCatogry;
            eventEntity.TicketMode = finalTicketMode;

           

            await _unitOfWork.SaveChangesAsync(cancellationToken);

           await _cacheVersionService.RefreshVersionAsync();

           
            return Result<EventResponse>.Ok(
                new EventResponse
                {
                    EventId = eventEntity.Id,
                    Name = eventEntity.Name,
                    Description = eventEntity.Description,
                    StartDate = eventEntity.StartDate,
                    EndDate = eventEntity.EndDate,
                    Status = eventEntity.Status,
                    Price = eventEntity.Price,
                    AvailableQuantity = eventEntity.Capacity,
                    Category= eventEntity.Category,
                    TicketMode = eventEntity.TicketMode,

                    TicketCategories = eventEntity.TicketCategories
                        .Select(tc => new TicketCategoryResponse
                        {
                            CategoryId = tc.Id,
                            Name = tc.Name,
                            Price = tc.Price,
                            AvailableQuantity = tc.Capacity
                        })
                        .ToList()
                });
        }
    }
}