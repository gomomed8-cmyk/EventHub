using System.Globalization;
using System.Text.Encodings.Web;
using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Email;
using Scriban;

namespace EventHub.Infrastructure.Services.Email;

public class EmailTemplateRenderer: IEmailTemplateRenderer
{
    private readonly string _templatePath;

    public EmailTemplateRenderer()
    {
        _templatePath = Path.Combine(
            AppContext.BaseDirectory,
            "Services",
            "Email",
            "Templates",
            "BookingConfirmationEmailTemplate.html");
    }

    public async Task<string> RenderBookingConfirmationAsync(
        BookingConfirmationEmailModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!File.Exists(_templatePath))
        {
            throw new FileNotFoundException(
                "Booking confirmation email template was not found.",
                _templatePath);
        }

        var templateContent = await File.ReadAllTextAsync(
            _templatePath,
            cancellationToken);

        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            var errors = string.Join(
                Environment.NewLine,
                template.Messages);

            throw new InvalidOperationException(
                $"Email template contains syntax errors:{Environment.NewLine}{errors}");
        }

        var htmlEncoder = HtmlEncoder.Default;

        var templateModel = new
        {
            customer_name = htmlEncoder.Encode(model.CustomerName ?? string.Empty),

            booking_id = model.BookingId,

            event_name = htmlEncoder.Encode(model.EventName ?? string.Empty),

            event_start_date = model.EventStartDate.ToString(
                "MMM dd, yyyy - hh:mm tt",
                CultureInfo.InvariantCulture),

            event_end_date = model.EventEndDate.ToString(
                "MMM dd, yyyy - hh:mm tt",
                CultureInfo.InvariantCulture),

            total_tickets = model.TotalTickets,

            total_amount = model.TotalAmount.ToString(
                "N2",
                CultureInfo.InvariantCulture),

            tickets = model.Tickets.Select(ticket => new
            {
                ticket_number = ticket.TicketNumber,

                ticket_code = htmlEncoder.Encode(
                    ticket.TicketCode ?? string.Empty),

                status = htmlEncoder.Encode(
                    ticket.Status ?? string.Empty)
            }).ToList()
        };

        return template.Render(templateModel);
    }
}