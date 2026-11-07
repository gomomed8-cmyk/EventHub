using EventHub.Application.Contracts;
using EventHub.Application.DTOs.Email;
using EventHub.Application.Specifications.Bookings;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.Services.Email;

public sealed class BookingConfirmationEmailJob
{
    private static readonly TimeSpan ClaimLeaseDuration =
        TimeSpan.FromMinutes(10);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICustomerInfoLookup _customerInfoLookup;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ITicketPdfGenerator _ticketPdfGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingConfirmationEmailJob> _logger;

    public BookingConfirmationEmailJob(
        IUnitOfWork unitOfWork,
        ICustomerInfoLookup customerInfoLookup,
        IEmailTemplateRenderer templateRenderer,
        ITicketPdfGenerator ticketPdfGenerator,
        IEmailService emailService,
        ILogger<BookingConfirmationEmailJob> logger)
    {
        _unitOfWork = unitOfWork;
        _customerInfoLookup = customerInfoLookup;
        _templateRenderer = templateRenderer;
        _ticketPdfGenerator = ticketPdfGenerator;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(
    Attempts = 5,
    DelaysInSeconds = new[] { 60, 120, 300, 600, 900 })]
    public async Task SendBookingConfirmationAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var bookingRepository =
            _unitOfWork.GetRepository<Booking, Guid>();

        var booking = await bookingRepository.GetByIdAsync(
            new GetBookingForConfirmationEmailSpecification(bookingId),
            cancellationToken);

        if (booking is null)
        {
            _logger.LogWarning(
                "Booking {BookingId} was not found. Confirmation email skipped.",
                bookingId);
            return;
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            _logger.LogWarning(
                "Booking {BookingId} is not confirmed. Email skipped.",
                bookingId);
            return;
        }

        if (booking.TicketsEmailSentAt.HasValue)
        {
            _logger.LogInformation(
                "Confirmation email already sent for booking {BookingId}.",
                bookingId);
            return;
        }

        var now = DateTime.UtcNow;

        if (booking.TicketsEmailClaimedAt.HasValue &&
            now - booking.TicketsEmailClaimedAt.Value < ClaimLeaseDuration)
        {
            _logger.LogInformation(
                "Booking {BookingId} is already claimed by another worker.",
                bookingId);
            throw new InvalidOperationException(
                $"Booking {bookingId} is currently claimed. Retry after the claim lease expires.");
        }

        booking.TicketsEmailClaimedAt = now;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogInformation(
                ex,
                "Another worker acquired the email claim for booking {BookingId}.",
                bookingId);
            return;
        }

        var emailSendAttempted = false;

        try
        {
            var tickets = booking.BookingItems
                .SelectMany(item => item.Tickets)
                .OrderBy(ticket => ticket.TicketNumber)
                .ToList();

            if (tickets.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No tickets found for booking {bookingId}.");
            }

            var customerInfo = await _customerInfoLookup.GetByUserIdsAsync(
                new[] { booking.UserId },
                cancellationToken);

            if (!customerInfo.TryGetValue(
                    booking.UserId,
                    out var customer) ||
                string.IsNullOrWhiteSpace(customer.Email))
            {
                throw new InvalidOperationException(
                    $"Customer email not found for booking {bookingId}.");
            }

            var emailModel = new BookingConfirmationEmailModel
            {
                CustomerName = customer.DisplayName ?? "Customer",
                BookingId = booking.Id,
                EventName = booking.Event.Name,
                EventStartDate = booking.Event.StartDate,
                EventEndDate = booking.Event.EndDate,
                TotalTickets = tickets.Count,
                TotalAmount = booking.TotalAmount,

                Tickets = tickets.Select(ticket => new TicketEmailModel
                {
                    TicketNumber = ticket.TicketNumber,
                    TicketCode = ticket.TicketCode,
                    Status = ticket.Status.ToString()
                }).ToList()
            };

            var htmlBody =
                await _templateRenderer.RenderBookingConfirmationAsync(
                    emailModel,
                    cancellationToken);

            var pdfBytes =
                await _ticketPdfGenerator.GenerateBookingTicketsAsync(
                    emailModel,
                    cancellationToken);

            var fileName = $"EventHub-Tickets-{booking.Id:N}.pdf";

            emailSendAttempted = true;

            await _emailService.SendEmailWithAttachmentAsync(
                customer.Email,
                $"Booking Confirmation - {booking.Event.Name}",
                htmlBody,
                pdfBytes,
                fileName,
                cancellationToken);

            booking.TicketsEmailSentAt = DateTime.UtcNow;
            booking.TicketsEmailClaimedAt = null;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Booking confirmation email sent successfully for booking {BookingId}.",
                bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process confirmation email for booking {BookingId}.",
                bookingId);

            if (!emailSendAttempted)
            {
                await ReleaseClaimAsync(booking, bookingId);
            }

            throw;
        }
    }

    private async Task ReleaseClaimAsync(
        Booking booking,
        Guid bookingId)
    {
        try
        {
            booking.TicketsEmailClaimedAt = null;

            await _unitOfWork.SaveChangesAsync(
                CancellationToken.None);

            _logger.LogInformation(
                "Email claim released for booking {BookingId}.",
                bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to release email claim for booking {BookingId}.",
                bookingId);
        }
    }
}
