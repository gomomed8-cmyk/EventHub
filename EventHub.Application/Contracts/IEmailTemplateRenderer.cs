using EventHub.Application.DTOs.Email;

namespace EventHub.Application.Contracts;

public interface IEmailTemplateRenderer
{
    Task<string> RenderBookingConfirmationAsync(BookingConfirmationEmailModel model,CancellationToken cancellationToken = default);
}