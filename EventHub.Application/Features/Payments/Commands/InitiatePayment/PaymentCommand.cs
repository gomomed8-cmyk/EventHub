
using EventHub.Application.DTOs.Payments;
using MediatR;

namespace EventHub.Application.Features.Payment.Commands.InitiatePayment
{
    public sealed record PaymentCommand(
        Guid BookingId,
        PaymentRequest Request,
        string? IdempotencyKey)
        : IRequest<Result<PaymentResponse>>;
}
