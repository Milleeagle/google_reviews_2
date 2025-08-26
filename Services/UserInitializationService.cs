using Microsoft.AspNetCore.Identity;

namespace google_reviews.Services
{
    public class UserInitializationService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserInitializationService> _logger;

        public UserInitializationService(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<UserInitializationService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            // Create roles
            await CreateRoleIfNotExistsAsync("Admin");
            await CreateRoleIfNotExistsAsync("User");

            // Create admin user
            await CreateAdminUserAsync();
        }

        private async Task CreateRoleIfNotExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation($"Created role: {roleName}");
            }
        }

        private async Task CreateAdminUserAsync()
        {
            var adminEmail = _configuration["AdminUser:Email"];
            var adminPassword = _configuration["AdminUser:Password"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogWarning("Admin user credentials not configured. Please set AdminUser:Email and AdminUser:Password in configuration.");
                return;
            }

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
            {
                return; // Admin user already exists
            }

            var adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation($"Created admin user: {adminEmail}");
            }
            else
            {
                _logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}