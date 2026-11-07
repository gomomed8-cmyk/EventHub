using EventHub.Infrastructure.Identity.EntityIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;



namespace EventHub.Infrastructure.Identity.ContextIdentity
{
    internal class EventHubIdentityDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>,Guid>
    {
        public EventHubIdentityDbContext(DbContextOptions<EventHubIdentityDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(100);
            });
        }
    }
}