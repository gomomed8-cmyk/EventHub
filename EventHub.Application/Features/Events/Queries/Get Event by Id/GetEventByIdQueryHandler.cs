using EventHub.Application.Common.Errors;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Specifications.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using MediatR;


namespace EventHub.Application.Features.Events.Queries.Get_Event_by_Id
{
    public sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetEventByIdQueryHandler(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<EventResponse>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            var spec= new EventByIdSpecification(request.EventId);

            var eventData= await _unitOfWork.GetRepository<Event,Guid>().GetByIdAsync(spec,cancellationToken);
            if (eventData == null)
            {
                return Result<EventResponse>.Fail(new Error
                    ("Event.NotFound", $"Event with Id {request.EventId} not found.",
                    ErrorType.NotFound));
            }
            
            return Result<EventResponse>.Ok(new EventResponse
            {
                EventId = eventData.Id,
                Name = eventData.Name,
                Description = eventData.Description,
                StartDate = eventData.StartDate,
                EndDate = eventData.EndDate,
                Status = eventData.Status,

                Price = eventData.TicketMode == TicketMode.GeneralAdmission
        ? eventData.Price
        : null,

                TicketMode = eventData.TicketMode,
                Category = eventData.Category,

                AvailableQuantity = eventData.TicketMode == TicketMode.GeneralAdmission
        ? eventData.AvailableQuantity
        : eventData.TicketCategories.Sum(tc => tc.AvailableQuantity),

                TicketCategories = eventData.TicketCategories.Select(tc => new TicketCategoryResponse
                {
                    CategoryId = tc.Id,
                    Name = tc.Name,
                    Price = tc.Price,
                    AvailableQuantity = tc.AvailableQuantity
                }).ToList()
            });
        }
    }
}
