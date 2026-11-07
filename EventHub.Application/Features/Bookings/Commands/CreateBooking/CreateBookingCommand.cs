using EventHub.Application.DTOs.Booking;
using MediatR;


namespace EventHub.Application.Features.Bookings.Commands.CreateBooking
{
    public sealed record CreateBookingCommand(CreateBookingRequest Request, string IdempotencyKey) 
        :IRequest<Result<CreateBookingResponse>>
    {
    }
}
