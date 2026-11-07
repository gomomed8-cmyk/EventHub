using EventHub.Application.DTOs.Email;

namespace EventHub.Application.Contracts;

public interface ITicketPdfGenerator
{
    Task<byte[]> GenerateBookingTicketsAsync(BookingConfirmationEmailModel model,CancellationToken cancellationToken = default);
}