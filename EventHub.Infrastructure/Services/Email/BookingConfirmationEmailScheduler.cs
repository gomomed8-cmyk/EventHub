using EventHub.Application.Contracts;
using Hangfire;

namespace EventHub.Infrastructure.Services.Email;

internal sealed class BookingConfirmationEmailScheduler : IBookingConfirmationEmailScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BookingConfirmationEmailScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public void Schedule(Guid bookingId)
    {
        _backgroundJobClient.Enqueue<BookingConfirmationEmailJob>(
            job => job.SendBookingConfirmationAsync(
                bookingId,
                CancellationToken.None));
    }
}