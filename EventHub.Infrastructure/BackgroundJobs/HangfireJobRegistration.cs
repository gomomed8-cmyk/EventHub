using EventHub.Application.Contracts;
using Hangfire;

namespace EventHub.Infrastructure.BackgroundJobs
{
    public static class HangfireJobRegistration
    {
        public static void RegisterRecurringJobs(IRecurringJobManager recurringJobManager)
        {
            recurringJobManager.AddOrUpdate<IEventMaintenanceService>(
                "complete-expired-events",
                    service => service.CompleteExpiredEventsAsync(CancellationToken.None),
                  Cron.MinuteInterval(5));


            recurringJobManager.AddOrUpdate<IBookingExpirationService>
                ("Processing-expired-booking",
                service => service.ProcessingExpiredBookingsAsync(CancellationToken.None)
                , Cron.MinuteInterval(1)
                );
        }
    }
}