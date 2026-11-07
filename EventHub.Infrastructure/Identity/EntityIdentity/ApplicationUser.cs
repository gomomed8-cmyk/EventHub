using Microsoft.AspNetCore.Identity;
namespace EventHub.Infrastructure.Identity.EntityIdentity
{
    public class ApplicationUser: IdentityUser<Guid>
    {
        public string DisplayName { get; set; }=default!;
    }
}
