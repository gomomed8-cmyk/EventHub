using EventHub.Application.Common.Results;
using EventHub.Application.DTOs.Booking;
using EventHub.Application.Features.Bookings.Commands.CancelBooking;
using EventHub.Application.Features.Bookings.Commands.CreateBooking;
using EventHub.Application.Features.Bookings.Queries.Get_Bookings_by_Id;
using EventHub.Application.Features.Bookings.Queries.GetEventBookings;
using EventHub.Application.Features.Bookings.Queries.GetMyBookings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EventHub.API.Controllers
{
   
    public class BookingController : ApiBaseController
    {
        private readonly IMediator _mediator;
        public BookingController(IMediator mediator) 
        {
         _mediator = mediator;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CreateBookingResponse>> CreateBooking([FromBody]CreateBookingRequest request, 
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken ct)
        {
            var command=new CreateBookingCommand(request,idempotencyKey ?? string.Empty);
            var result = await _mediator.Send(command);
            return ToActionResult(result);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<BookingListResponse>>> GetMyBookings(
            [FromQuery]GetMyBookingsQuery query,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);

            return ToActionResult(result);
        }

        [Authorize]
        [HttpGet("{bookingId:guid}")]
        public async Task<ActionResult<BookingDetailsResponse>> GetBookingById(Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBookingByIdQuery(bookingId),cancellationToken);
            return ToActionResult(result);
        }

        [Authorize]
        [HttpPost("{bookingId:guid}/cancel")]
        public async Task<ActionResult<Unit>> CancelBooking(Guid bookingId, CancellationToken cancellationToken)
        {
            var result=await _mediator.Send(new CancelBookingCommand(bookingId),cancellationToken);

            if (result.IsSuccess)
                return NoContent();

            return ToActionResult(result);
        }

       

    }
}
