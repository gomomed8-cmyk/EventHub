


namespace EventHub.Infrastructure.Identity.DataSeeding
{
    public class AdminSeedOptions
    {
        public string DisplayName { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; }=default!;
        public string PhoneNumber { get; set; } = default!;
    }
}
