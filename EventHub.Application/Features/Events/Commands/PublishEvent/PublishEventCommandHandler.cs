using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Features.Events.Commands.PublishEvent
{
    public sealed class PublishEventCommandHandler
        : IRequestHandler<PublishEventCommand, Result<PublishEventCommandResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public PublishEventCommandHandler(IUnitOfWork unitOfWork,ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PublishEventCommandResponse>> Handle(PublishEventCommand request,CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
            var spec= new PublishEventSpecification(request.EventId);
            var eventEntity = await _unitOfWork
                .GetRepository<Event, Guid>()
                .GetByIdAsync(spec,cancellationToken);

            if (eventEntity == null)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event not found",
                        "Event Not Found",
                        ErrorType.NotFound));
            }
            if (!_currentUserService.IsInRole("Admin") &&eventEntity.OrganizerId != currentUserId)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event.Forbidden",
                        "You are not allowed to publish this event.",
                        ErrorType.Forbidden));
            }
            if (eventEntity.Status != EventStatus.Draft)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event cannot be published in its current status",
                        "Event Cannot Be Published",
                        ErrorType.Conflict));
            }
            if (eventEntity.TicketMode != TicketMode.GeneralAdmission&&eventEntity.TicketMode!=TicketMode.Categorized)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event cannot be published with its current ticket mode",
                        "Event Cannot Be Published",
                        ErrorType.Conflict));

            }
            if(eventEntity.TicketMode==TicketMode.GeneralAdmission)
            {
                if (eventEntity.Price < 0)
                {
                    return Result<PublishEventCommandResponse>.Fail(
                        new Error(
                            "Event cannot be published with its current price",
                            "Event Cannot Be Published",
                            ErrorType.Conflict));
                }
              if (eventEntity.Capacity <= 0)
              {
                    return Result<PublishEventCommandResponse>.Fail(
                        new Error(
                            "Event cannot be published with its current capacity",
                            "Event Cannot Be Published",
                            ErrorType.Conflict));
              }
            }
            if(eventEntity.TicketMode==TicketMode.Categorized)
            {
                if (eventEntity.TicketCategories == null ||!eventEntity.TicketCategories.Any())
                {
                    return Result<PublishEventCommandResponse>.Fail(
                        new Error(
                            "At least one ticket category is required.",
                            "Event.TicketCategories.Required",
                            ErrorType.Conflict));
                }

                foreach (var category in eventEntity.TicketCategories)
                {
                    if (string.IsNullOrWhiteSpace(category.Name))
                    {
                        return Result<PublishEventCommandResponse>.Fail(
                            new Error(
                                "Ticket category name is required.",
                                "Event.TicketCategory.NameRequired",
                                ErrorType.Conflict));
                    }

                    if (category.Capacity <= 0)
                    {
                        return Result<PublishEventCommandResponse>.Fail(
                            new Error(
                                "Ticket category capacity must be greater than zero.",
                                "Event.TicketCategory.InvalidCapacity",
                                ErrorType.Conflict));
                    }

                    if (category.Price < 0)
                    {
                        return Result<PublishEventCommandResponse>.Fail(
                            new Error(
                                "Ticket category price cannot be negative.",
                                "Event.TicketCategory.InvalidPrice",
                                ErrorType.Conflict));
                    }
                }
            }

           if(eventEntity.StartDate < DateTime.UtcNow)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event cannot be published because the start date has already passed.",
                        "Event Cannot Be Published",
                        ErrorType.Conflict));
            }
           if(eventEntity.EndDate < eventEntity.StartDate)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event cannot be published because the end date is before the start date.",
                        "Event Cannot Be Published",
                        ErrorType.Conflict));
            }
           if(eventEntity.EndDate < DateTime.UtcNow)
            {
                return Result<PublishEventCommandResponse>.Fail(
                    new Error(
                        "Event cannot be published because the end date has already passed.",
                        "Event Cannot Be Published",
                        ErrorType.Conflict));
            }

            eventEntity.Status = EventStatus.Published;
            _unitOfWork
                .GetRepository<Event, Guid>()
                .Update(eventEntity);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<PublishEventCommandResponse>.Ok(
                new PublishEventCommandResponse
                {
                    EventId = eventEntity.Id,
                    Status = eventEntity.Status
                });
        }
    }
}