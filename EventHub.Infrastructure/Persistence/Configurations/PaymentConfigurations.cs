using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    internal class PaymentConfigurations
        : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(p => p.Currency)
                .HasMaxLength(3)
                .IsRequired()
                .HasDefaultValue("EGP");

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.PaymobIntentionId)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(p => p.PaymobOrderId)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(p => p.PaymobTransactionId)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(p => p.CheckoutUrl)
              .HasMaxLength(2048)
              .IsRequired(false);

            builder.Property(p => p.BookingId)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired(false);

            builder.Property(p => p.PaymentMethod)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(p => p.IdempotencyKey)
                .HasMaxLength(100)
                .IsRequired(false);
            builder.Property(p => p.RowVersion)
                .IsRowVersion();

            builder.Property(p => p.FailureReason)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.BookingId);

            builder.HasIndex(p => p.PaymobIntentionId)
                .IsUnique()
                .HasFilter("[PaymobIntentionId] IS NOT NULL");

            builder.HasIndex(p => p.PaymobOrderId)
                .IsUnique()
                .HasFilter("[PaymobOrderId] IS NOT NULL");

            builder.HasIndex(p => p.PaymobTransactionId)
                .IsUnique()
                .HasFilter("[PaymobTransactionId] IS NOT NULL");

            builder.HasIndex(p => new
            {
                p.BookingId,
                p.IdempotencyKey
            })
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
        }
    }
}