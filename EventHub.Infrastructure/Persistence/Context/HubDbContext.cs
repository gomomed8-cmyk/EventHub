using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Context
{
    public class HubDbContext(DbContextOptions<HubDbContext> options)
        : DbContext(options)
    {
        public DbSet<Booking> Bookings { get; set; }

        public DbSet<BookingItem> BookingItems { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<TicketCategory> TicketCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(HubDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}