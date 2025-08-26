using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using google_reviews.Data;

namespace google_reviews.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DiagnosticsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            try 
            {
                var diagnostics = new
                {
                    DatabaseConnected = await CanConnectToDatabase(),
                    TablesExist = await TablesExist(),
                    RolesExist = await RolesExist(),
                    AdminUserExists = await AdminUserExists(),
                    UsersCount = _userManager.Users.Count(),
                    ErrorMessage = ""
                };

                ViewBag.Diagnostics = diagnostics;
                return View();
            }
            catch (Exception ex)
            {
                var diagnostics = new
                {
                    DatabaseConnected = false,
                    TablesExist = false,
                    RolesExist = false,
                    AdminUserExists = false,
                    UsersCount = 0,
                    ErrorMessage = ex.Message
                };

                ViewBag.Diagnostics = diagnostics;
                return View();
            }
        }

        private async Task<bool> CanConnectToDatabase()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TablesExist()
        {
            try
            {
                await _context.Users.CountAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RolesExist()
        {
            return await _roleManager.RoleExistsAsync("Admin") && await _roleManager.RoleExistsAsync("User");
        }

        private async Task<bool> AdminUserExists()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return adminUsers.Any();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdminUser()
        {
            try
            {
                // Create admin user manually
                var adminUser = new IdentityUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, "AdminPassword123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    TempData["Success"] = "Admin user created successfully!";
                }
                else
                {
                    TempData["Error"] = $"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating admin user: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}