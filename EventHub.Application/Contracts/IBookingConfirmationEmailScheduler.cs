namespace EventHub.Application.Contracts;

public interface IBookingConfirmationEmailScheduler
{
    void Schedule(Guid bookingId);
}