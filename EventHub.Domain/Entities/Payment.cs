using EventHub.Domain.Common;
using EventHub.Domain.Enums;

namespace EventHub.Domain.Entities
{
    public class Payment : BaseEntity<Guid>
    {
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "EGP";

        public PaymentStatus Status { get; set; }

        public string? PaymobIntentionId { get; set; }

        public string? PaymobOrderId { get; set; }

        public string? PaymobTransactionId { get; set; }
        public string? CheckoutUrl { get; set; }
        public Guid BookingId { get; set; }

        public Booking Booking { get; set; } = default!;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? PaymentMethod { get; set; }

        public string? IdempotencyKey { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string? FailureReason { get; set; }
    }
}