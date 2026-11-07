using AutoMapper;
using EventHub.Application.Common;
using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;


namespace EventHub.Application.Features.Events.Commands.Create_Event
{
    public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<EventResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        public CreateEventCommandHandler(IUnitOfWork unitOfWork,IMapper mapper,ICurrentUserService currentUserService) 
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<EventResponse>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
        {
           var userId= _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                return UserErrors.Unauthorized;
            }
        
            var eventEntity = _mapper.Map<Event>(request.Request);
            if (eventEntity.TicketMode == TicketMode.GeneralAdmission)
            {
                if (eventEntity.TicketCategories.Any())
                {
                    return Result<EventResponse>.Fail(
                        new Error(
                            "Event.CategoriesNotAllowed",
                            "General admission events cannot have ticket categories.",
                            ErrorType.Conflict));
                }

                if (eventEntity.Capacity <= 0 || eventEntity.Price < 0)
                {
                    return Result<EventResponse>.Fail(
                        new Error(
                            "Event.InvalidPricing",
                            "Event capacity must be greater than 0 and price must be non-negative.",
                            ErrorType.Conflict));
                }
            }
            else if (eventEntity.TicketMode == TicketMode.Categorized)
            {
                if (!eventEntity.TicketCategories.Any())
                {
                    return Result<EventResponse>.Fail(
                        new Error(
                            "Event.NoTicketCategories",
                            "Categorized events must have at least one ticket category.",
                            ErrorType.Conflict));
                }

                foreach (var category in eventEntity.TicketCategories)
                {
                    if (string.IsNullOrWhiteSpace(category.Name))
                    {
                        return Result<EventResponse>.Fail(
                            new Error(
                                "TicketCategory.InvalidName",
                                "Ticket category name is required.",
                                ErrorType.Conflict));
                    }

                    if (category.Capacity <= 0 || category.Price < 0)
                    {
                        return Result<EventResponse>.Fail(
                            new Error(
                                "TicketCategory.InvalidPricing",
                                "Ticket category capacity must be greater than 0 and price must be non-negative.",
                                ErrorType.Conflict));
                    }
                    category.AvailableQuantity=category.Capacity;
                }
                
                eventEntity.Capacity = eventEntity.TicketCategories.Sum(tc => tc.Capacity);
                eventEntity.Price=0;
            }

            if (eventEntity.TicketMode == TicketMode.GeneralAdmission)
            {
                eventEntity.AvailableQuantity = eventEntity.Capacity;
            }
            else if (eventEntity.TicketMode == TicketMode.Categorized)
            {
                eventEntity.AvailableQuantity = eventEntity.TicketCategories
                    .Sum(tc => tc.AvailableQuantity);
            }

            if (eventEntity.StartDate < DateTime.UtcNow)
            {
                return EventErrors.InvalidStartDate;
            }
            if (eventEntity.EndDate <= eventEntity.StartDate)
            {
                return EventErrors.InvalidDateRange;
            }

            eventEntity.Status = EventStatus.Draft;
            eventEntity.OrganizerId = userId;

            eventEntity.DuplicateFingerprint =
    EventFingerprintGenerator.Generate(eventEntity);

            var duplicateSpec = new DuplicateEventSpecification(eventEntity);

            var existingEvents = await _unitOfWork
                .GetRepository<Event, Guid>()
                .GetAllAsync(duplicateSpec, cancellationToken);

            bool duplicateExists = existingEvents.Any(existing =>
                existing.TicketCategories.Count ==
                    eventEntity.TicketCategories.Count &&
                existing.TicketCategories.All(existingCategory =>
                    eventEntity.TicketCategories.Any(newCategory =>
                        string.Equals(
                            existingCategory.Name.Trim(),
                            newCategory.Name.Trim(),
                            StringComparison.OrdinalIgnoreCase) &&
                        existingCategory.Price == newCategory.Price &&
                        existingCategory.Capacity == newCategory.Capacity)));

            if (duplicateExists)
            {
                return Result<EventResponse>.Fail(
                    new Error(
                        "Event.Duplicate",
                        "An identical event already exists.",
                        ErrorType.Conflict));
            }


            _unitOfWork.GetRepository<Event,Guid>().Add(eventEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<EventResponse>.Ok(_mapper.Map<EventResponse>(eventEntity));

        }
    }
}
