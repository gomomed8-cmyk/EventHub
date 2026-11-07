using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    internal class BookingItemConfigurations
        : IEntityTypeConfiguration<BookingItem>
    {
        public void Configure(EntityTypeBuilder<BookingItem> builder)
        {
            builder.HasKey(bi => bi.Id);

            builder.Property(bi => bi.BookingId)
                .IsRequired();

            builder.Property(bi => bi.TicketCategoryId)
                .IsRequired(false);

            builder.Property(bi => bi.UnitPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(bi => bi.TotalPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(bi => bi.Quantity)
                .IsRequired();

            builder.HasOne(bi => bi.Booking)
                .WithMany(b => b.BookingItems)
                .HasForeignKey(bi => bi.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(bi => bi.TicketCategory)
                .WithMany()
                .HasForeignKey(bi => bi.TicketCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(bi => bi.TicketCategoryId);
        }
    }
}