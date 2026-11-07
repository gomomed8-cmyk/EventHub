using EventHub.Application.DTOs.Booking;
using MediatR;

namespace EventHub.Application.Features.Bookings.Queries.Get_Bookings_by_Id
{
    public sealed record GetBookingByIdQuery(Guid BookingId):IRequest<Result<BookingDetailsResponse>>
    {

    }
}
