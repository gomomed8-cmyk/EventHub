using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.DTOs.Events;
using EventHub.Application.Features.Bookings.Queries.GetEventBookings;
using EventHub.Application.Features.Bookings.Queries.GetEventBookingSummary;
using EventHub.Application.Features.Events.Commands.CancelEvent;
using EventHub.Application.Features.Events.Commands.Create_Event;
using EventHub.Application.Features.Events.Commands.PublishEvent;
using EventHub.Application.Features.Events.Commands.UpdateEvent;
using EventHub.Application.Features.Events.Queries.Get_Event_by_Id;
using EventHub.Application.Features.Events.Queries.GetAllEvent;
using EventHub.Application.Features.Events.Queries.GetMyEvents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.API.Controllers
{
   
    public class EventController : ApiBaseController
    {
        private readonly IMediator _mediator;
        public EventController(IMediator mediator) 
        { 
         _mediator = mediator;
        }

        [Authorize(Roles = "Organizer")]
        [HttpPost]
        public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventCommand command,CancellationToken ct)
        {
            var result = await _mediator.Send(command,ct);
            return ToActionResult(result);
        }
        [HttpGet]
        public async Task<ActionResult<PagedResult<EventListResponse>>> GetAllEvents([FromQuery] GetAllEventsQuery query,CancellationToken ct)
        {
            var result = await _mediator.Send(query,ct);
            return ToActionResult(result);
        }

        [HttpGet("{eventId:guid}")]
        public async Task<ActionResult<EventResponse>> GetEventById(Guid eventId,CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEventByIdQuery(eventId),ct);
            return ToActionResult(result);
        }

        [Authorize(Roles ="Organizer")]
        [HttpGet("my-events")]
        public async Task<ActionResult<PagedResult<EventListResponse>>> GetMyEvents([FromQuery] GetMyEventsQuery query,CancellationToken ct)
        {
            var result =await _mediator.Send(query,ct);
            return ToActionResult(result);
        }

        [Authorize(Roles = "Admin,Organizer")]
        [HttpPost("{eventId:guid}/publish")]
        public async Task<ActionResult<PublishEventCommandResponse>> PublishEvent(Guid eventId,CancellationToken ct)
        {
            var result =await _mediator.Send(new PublishEventCommand(eventId),ct);
            return ToActionResult(result);
        }

        [Authorize(Roles = "Admin,Organizer")]
        [HttpPatch("{eventId:guid}")]
        public async Task<ActionResult<EventResponse>> UpdateEvent(Guid eventId, [FromBody] UpdateEventRequest updateEventRequest,CancellationToken ct)
        {
            var command = new UpdateEventCommand(eventId, updateEventRequest);
            var result =await _mediator.Send(command,ct);
            return ToActionResult(result);
        }

        [Authorize(Roles = "Admin,Organizer")]
        [HttpPost("{eventId:guid}/cancel")]
        public async Task<ActionResult<Unit>> CancelEvent(Guid eventId,CancellationToken ct)
        {
            var result =await _mediator.Send(new CancelEventCommand(eventId),ct);

            if (result.IsSuccess)
                return NoContent();

            return ToActionResult(result);
        }

        [Authorize(Roles = "Admin,Organizer")]
        [HttpGet("{eventId:guid}/bookings")]
        public async Task<ActionResult<PagedResult<OrganizerBookingListResponse>>> GetEventBookings(Guid eventId,
            [FromQuery] GetEventBookingsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query with { EventId = eventId },
                cancellationToken);

            return ToActionResult(result);
        }

        [Authorize(Roles = "Admin,Organizer")]
        [HttpGet("{eventId:guid}/booking-summary")]
        public async Task<ActionResult<EventBookingSummaryResponse>> GetEventBookingSummary( Guid eventId,CancellationToken cancellationToken)
        {
            var query = new GetEventBookingSummaryQuery(eventId);

            var result = await _mediator.Send(query, cancellationToken);

            return ToActionResult(result);
        }
    }
}
