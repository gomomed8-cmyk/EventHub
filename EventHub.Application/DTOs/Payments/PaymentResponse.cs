using EventHub.Domain.Enums;

namespace EventHub.Application.DTOs.Payments
{
    public sealed record PaymentResponse(
        Guid PaymentId,
        Guid BookingId,
        PaymentStatus Status,
        string CheckoutUrl,
        DateTime? ExpiresAt);
}