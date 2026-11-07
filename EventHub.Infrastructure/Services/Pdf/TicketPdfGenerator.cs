using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Email;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace EventHub.Infrastructure.Services.Pdf;

internal sealed class TicketPdfGenerator : ITicketPdfGenerator
{
    public Task<byte[]> GenerateBookingTicketsAsync(
        BookingConfirmationEmailModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        cancellationToken.ThrowIfCancellationRequested();

        if (model.Tickets.Count == 0)
            throw new InvalidOperationException(
                "Cannot generate tickets PDF without tickets.");

        if (model.Tickets.Any(ticket =>
                string.IsNullOrWhiteSpace(ticket.TicketCode)))
        {
            throw new InvalidOperationException(
                "Every ticket must have a valid ticket code.");
        }

        using var qrGenerator = new QRCodeGenerator();

        var qrCodes = model.Tickets
            .Select(ticket =>
            {
                using var qrData = qrGenerator.CreateQrCode(
                    ticket.TicketCode,
                    QRCodeGenerator.ECCLevel.Q);

                var qrCode = new PngByteQRCode(qrData);

                return qrCode.GetGraphic(10);
            })
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();

        var document = Document.Create(container =>
        {
            for (var i = 0; i < model.Tickets.Count; i++)
            {
                var ticket = model.Tickets[i];
                var qrCode = qrCodes[i];

                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(24);
                    page.PageColor(Colors.White);

                    page.Content()
                        .ShowEntire()
                        .AlignCenter()
                        .AlignMiddle()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(20)
                        .Column(column =>
                        {
                            column.Spacing(7);

                            column.Item()
                                .AlignCenter()
                                .Text("EVENTHUB")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item()
                                .AlignCenter()
                                .Text("EVENT TICKET")
                                .FontSize(12)
                                .SemiBold()
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .PaddingVertical(5)
                                .LineHorizontal(1)
                                .LineColor(Colors.Grey.Lighten2);

                            column.Item()
                                .Text(model.EventName)
                                .FontSize(18)
                                .Bold()
                                .AlignCenter();

                            column.Item()
                                .Text($"Customer: {model.CustomerName}")
                                .FontSize(10);

                            column.Item()
                                .Text($"Booking ID: {model.BookingId}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .Text(
                                    $"Start: {model.EventStartDate:MMM dd, yyyy - hh:mm tt}")
                                .FontSize(9);

                            column.Item()
                                .Text(
                                    $"End: {model.EventEndDate:MMM dd, yyyy - hh:mm tt}")
                                .FontSize(9);

                            
                            column.Item()
                                .PaddingVertical(5)
                                .AlignCenter()
                                .Width(140)
                                .Height(140)
                                .Image(qrCode)
                                .FitArea();

                            column.Item()
                                .AlignCenter()
                                .Text($"Ticket #{ticket.TicketNumber}")
                                .FontSize(15)
                                .Bold();

                            column.Item()
                                .AlignCenter()
                                .Text($"Ticket Code: {ticket.TicketCode}")
                                .FontSize(8)
                                .SemiBold();

                            column.Item()
                                .AlignCenter()
                                .Text($"Status: {ticket.Status}")
                                .FontSize(9);

                            column.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Text("Present this ticket at the event entrance.")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                });
            }
        });

        var pdfBytes = document.GeneratePdf();

        return Task.FromResult(pdfBytes);
    }
}
