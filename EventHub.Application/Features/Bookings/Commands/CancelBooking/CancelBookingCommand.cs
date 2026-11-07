using MediatR;


namespace EventHub.Application.Features.Bookings.Commands.CancelBooking
{
    public sealed record CancelBookingCommand(Guid BookingId):IRequest<Result<Unit>>
    {
    }
}
