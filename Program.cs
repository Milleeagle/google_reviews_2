using google_reviews.Data;
using google_reviews.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false; // Disable email confirmation for now
    })
    .AddRoles<IdentityRole>() // Add roles support
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<UserInitializationService>();
builder.Services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();
builder.Services.AddScoped<IGooglePlacesService, GooglePlacesService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Initialize roles and admin user
try 
{
    using (var scope = app.Services.CreateScope())
    {
        var initService = scope.ServiceProvider.GetRequiredService<UserInitializationService>();
        await initService.InitializeAsync();
    }
}
catch (Exception ex)
{
    // Log the error but don't crash the app
    Console.WriteLine($"Error initializing user system: {ex.Message}");
}

app.Run();
