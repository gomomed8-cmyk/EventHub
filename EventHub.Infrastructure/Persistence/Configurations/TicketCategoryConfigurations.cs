using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations
{
    internal class TicketCategoryConfigurations
        : IEntityTypeConfiguration<TicketCategory>
    {
        public void Configure(EntityTypeBuilder<TicketCategory> builder)
        {
            builder.HasKey(tc => tc.Id);

            builder.Property(tc => tc.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(tc => tc.EventId)
                .IsRequired();

            builder.Property(tc => tc.Capacity)
                .IsRequired();

            builder.Property(tc => tc.AvailableQuantity)
                 .IsRequired();
            builder.Property(tc => tc.RowVersion)
                   .IsRowVersion();

            builder.Property(tc => tc.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.HasOne(tc => tc.Event)
                .WithMany(e => e.TicketCategories)
                .HasForeignKey(tc => tc.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(tc => new { tc.EventId, tc.Name })
                .IsUnique();
        }
    }
}