using google_reviews.Data;
using google_reviews.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Use production SQL Server database
var connectionString = builder.Configuration.GetConnectionString("SqlServerConnection") ?? throw new InvalidOperationException("Connection string 'SqlServerConnection' not found.");

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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IScheduledMonitorService, ScheduledMonitorService>();
builder.Services.AddScoped<ILanguageDetectionService, LanguageDetectionService>();
builder.Services.AddScoped<IReviewManagementService, ReviewManagementService>();
// IReviewScraper is created manually when needed to avoid Chrome driver accumulation
builder.Services.AddSingleton<BatchProgressService>();
// Disabled to prevent automatic API calls - re-enable when needed
// builder.Services.AddHostedService<ScheduledMonitorBackgroundService>();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddControllersWithViews();

// Increase form value limit for batch operations (for large company databases)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueCountLimit = 50000; // Allow up to 50,000 form values (default is 1024)
});

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

// Configure static files
app.UseStaticFiles();

// Also serve static files from wwwroot with explicit configuration
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "",
    OnPrepareResponse = ctx =>
    {
        // Set proper content types
        var path = ctx.Context.Request.Path.Value?.ToLowerInvariant();
        if (path?.EndsWith(".css") == true)
        {
            ctx.Context.Response.Headers.ContentType = "text/css";
        }
        else if (path?.EndsWith(".js") == true)
        {
            ctx.Context.Response.Headers.ContentType = "application/javascript";
        }

        // Cache static files in production
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400");
        }
    }
});

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