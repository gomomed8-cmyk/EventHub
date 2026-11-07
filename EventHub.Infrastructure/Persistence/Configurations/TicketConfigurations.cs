using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    internal class TicketConfigurations
        : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.BookingItemId)
                .IsRequired();

            builder.Property(t => t.TicketNumber)
                .IsRequired();

            builder.Property(t => t.TicketCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Status)
                .IsRequired();

            builder.Property(t => t.IssuedAt)
                .IsRequired();

            builder.Property(t => t.UsedAt)
                .IsRequired(false);

            builder.HasOne(t => t.BookingItem)
                .WithMany(item => item.Tickets)
                .HasForeignKey(t => t.BookingItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.TicketCode)
                .IsUnique();

            builder.HasIndex(t => new
            {
                t.BookingItemId,
                t.TicketNumber
            })
                .IsUnique();
        }
    }
}