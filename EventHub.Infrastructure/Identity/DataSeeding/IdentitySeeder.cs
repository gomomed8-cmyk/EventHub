using EventHub.Infrastructure.Identity.EntityIdentity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace EventHub.Infrastructure.Identity.DataSeeding
{
    public class IdentitySeeder
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IdentitySeeder> _logger;
        private readonly IOptions<AdminSeedOptions> _options;

        public IdentitySeeder(RoleManager<IdentityRole<Guid>> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<IdentitySeeder> logger, IOptions<AdminSeedOptions> options)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
            _options = options;
        }


        public async Task SeedRolesAsync()
        {
            string[] roles =
            {
                   "User",
                   "Organizer",
                   "Admin"
            };
            foreach (var role in roles)
            {
                var roleExist = await _roleManager.RoleExistsAsync(role);
                if (!roleExist)
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }
        }


        public async Task SeedAdminAsync(CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(_options.Value.Email);
            if (user == null)
            {

                var result = new ApplicationUser
                {
                    DisplayName = _options.Value.DisplayName,
                    Email = _options.Value.Email,
                    UserName = _options.Value.UserName,
                    PhoneNumber = _options.Value.PhoneNumber,

                };
                var createResult = await _userManager.CreateAsync(result, _options.Value.Password);
                if (createResult.Succeeded)
                {
                    var adminUser = await _userManager.AddToRoleAsync(result, "Admin");
                    if (adminUser.Succeeded)
                    {
                        _logger.LogInformation($"Admin user created successfully: {_options.Value.Email}");
                    }
                    else

                        foreach (var error in adminUser.Errors)
                        {
                            _logger.LogError($"Error adding admin user to role: {error.Description}");
                        }
                }

                else
                {
                    foreach (var error in createResult.Errors)
                    {
                        _logger.LogError($"Error creating admin user: {error.Description}");
                    }

                }

            }
            else
            {
                _logger.LogInformation($"Admin user already exists: {_options.Value.Email}");

            }
        }
    }
}

