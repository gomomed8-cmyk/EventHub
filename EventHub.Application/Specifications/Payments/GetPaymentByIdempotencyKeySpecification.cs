
using PaymentEntity = EventHub.Domain.Entities.Payment;

namespace EventHub.Application.Specifications.Payments
{
    public sealed class GetPaymentByIdempotencyKeySpecification
        : Specification<PaymentEntity, Guid>
    {
        public GetPaymentByIdempotencyKeySpecification(
            Guid bookingId,
            string idempotencyKey)
            : base(p =>
                p.BookingId == bookingId &&
                p.IdempotencyKey == idempotencyKey)
        {
        }
    }
}