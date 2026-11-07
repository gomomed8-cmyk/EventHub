using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    internal class BookingConfigurations
        : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.UserId)
                .IsRequired();

            builder.Property(b => b.EventId)
                .IsRequired();

            builder.Property(b => b.Status)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.ExpiresAt)
                .IsRequired(false);

            builder.Property(b => b.TicketsEmailSentAt)
                .IsRequired(false);

            builder.Property(b => b.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(b => b.RowVersion)
                .IsRowVersion();

            builder.Property(b => b.TicketsEmailClaimedAt)
                .IsRequired(false);

            builder.Property(b => b.IdempotencyKey)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.BookingItems)
                .WithOne(bi => bi.Booking)
                .HasForeignKey(bi => bi.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(b => b.Payments)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(b => b.UserId);

            builder.HasIndex(b => b.EventId);

            builder.HasIndex(b => new
            {
                b.Status,
                b.ExpiresAt
            });

            builder.HasIndex(b => new
            {
                b.UserId,
                b.IdempotencyKey
            })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
        }
    }
}