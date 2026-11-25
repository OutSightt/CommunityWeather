using System.Security.Claims;
using CommunityWeather.Data;
using CommunityWeather.Models;
using CommunityWeather.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Secrets (database password, API keys) can live in appsettings.Local.json, which is git-ignored.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Every POST/PUT/DELETE must carry a valid anti-forgery token (protects forms against CSRF).
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddMemoryCache();

// MySQL (Pomelo EF Core provider)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// Settings bound from configuration
builder.Services.Configure<WeatherSettings>(builder.Configuration.GetSection("Weather"));
builder.Services.Configure<GoogleMapsSettings>(builder.Configuration.GetSection("GoogleMaps"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Application services
builder.Services.AddScoped<AccountService>();

// External integrations
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddHttpClient<EmailService>();

// Cookie authentication with role-based authorization
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "CommunityWeather.Auth";
        options.Cookie.HttpOnly = true;                       // not readable by JavaScript
        options.Cookie.SameSite = SameSiteMode.Lax;           // not sent on cross-site POSTs
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;                      // HTTPS only outside development

        // On every request, confirm the account still exists, is active, and still has the same role.
        // A disabled account or a changed role therefore takes effect immediately, not at cookie expiry.
        options.Events.OnValidatePrincipal = async context =>
        {
            var idText = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            var valid = false;

            if (int.TryParse(idText, out var id))
            {
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var current = await db.Users.AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new { u.IsActive, u.Role })
                    .FirstOrDefaultAsync();
                valid = current != null && current.IsActive && current.Role == role;
            }

            if (!valid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Create tables if they do not exist yet, then seed starter data (first administrator comes from configuration)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db, app.Configuration.GetSection("SeedAdmin").Get<SeedAdminSettings>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

// Pages shown to a signed-in user are never cached, so the Back button cannot reopen them after logout.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache";
            context.Response.Headers["Pragma"] = "no-cache";
        }
        return Task.CompletedTask;
    });
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
