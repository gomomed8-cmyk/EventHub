using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;
namespace EventHub.Application.Features.Events.Commands.CancelEvent
{
    public sealed class CancelEventCommandHandler : IRequestHandler<CancelEventCommand, Result<Unit>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublicEventsCacheVersionService _cacheVersionService;
        public CancelEventCommandHandler(IUnitOfWork unitOfWork,ICurrentUserService currentUserService,
            IPublicEventsCacheVersionService cacheVersionService)
        { 
            _unitOfWork = unitOfWork; 
            _currentUserService = currentUserService;
            _cacheVersionService = cacheVersionService;
        }
        
        public async Task<Result<Unit>> Handle(CancelEventCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId;
   
            var @event =await _unitOfWork.GetRepository<Event,Guid>()
                .GetByIdAsync(request.EventId,cancellationToken);
            if (@event == null)
            {
                return Result<Unit>.Fail(
                    new Error("Event not found", "Event.NotFound",
                    ErrorType.NotFound));
            }
            if (!_currentUserService.IsInRole("Admin") && @event.OrganizerId != currentUserId)
            {
                return Result<Unit>.Fail(
                    new Error("You are not authorized to cancel this event", "Event.Unauthorized",
                    ErrorType.Unauthorized));
            }

            if (@event.Status == EventStatus.Completed)
            {
                return Result<Unit>.Fail(
                    new Error(
                        "Event is already completed",
                        "Event.AlreadyCompleted",
                        ErrorType.Conflict));
            }

            if (@event.Status == EventStatus.Cancelled)
            {
                return Result<Unit>.Fail(
                    new Error(
                        "Event is already cancelled",
                        "Event.AlreadyCancelled",
                        ErrorType.Conflict));
            }

            if (@event.Status != EventStatus.Draft &&
                @event.Status != EventStatus.Published)
            {
                return Result<Unit>.Fail(
                    new Error(
                        "Event cannot be cancelled in its current status",
                        "Event.InvalidStatus",
                        ErrorType.Conflict));
            }
            @event.Status = EventStatus.Cancelled;

            _unitOfWork.GetRepository<Event,Guid>().Update(@event);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheVersionService.RefreshVersionAsync();

            return Result<Unit>.Ok(Unit.Value);
        }
    }
}
