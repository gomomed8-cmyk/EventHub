using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    internal class EventConfigurations : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.OrganizerId)
                .IsRequired();

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(e => e.Status)
                .IsRequired();

            builder.Property(e => e.StartDate)
                .IsRequired();

            builder.Property(e => e.EndDate)
                .IsRequired();

            builder.Property(e => e.Price)
              .HasPrecision(18, 2);

            builder.Property(e => e.Capacity)
                .IsRequired();
            builder.Property(e => e.TicketMode)
                .IsRequired();
            builder.Property(e => e.Category)
               .IsRequired();

            builder.HasMany(e => e.TicketCategories)
                .WithOne(tc => tc.Event)
                .HasForeignKey(tc => tc.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Bookings)
                .WithOne(b => b.Event)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(e => e.DuplicateFingerprint)
                 .HasMaxLength(64);

           builder.HasIndex(e => e.DuplicateFingerprint)
                .IsUnique();

            builder.Property(e => e.RowVersion)
                   .IsRowVersion();

            builder.HasIndex(e => e.OrganizerId);

            builder.HasIndex(e => e.StartDate);

            builder.HasIndex(e => e.Status);
        }
    }
}