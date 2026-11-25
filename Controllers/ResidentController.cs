using System.Security.Claims;
using CommunityWeather.Data;
using CommunityWeather.Models;
using CommunityWeather.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Controllers;

/// <summary>Resident dashboard. Administrators are not allowed here; they have their own dashboard.</summary>
[Authorize(Roles = Roles.Resident)]
public class ResidentController : Controller
{
    private readonly AppDbContext _db;
    private readonly WeatherService _weather;

    public ResidentController(AppDbContext db, WeatherService weather)
    {
        _db = db;
        _weather = weather;
    }

    public async Task<IActionResult> Index()
    {
        var user = await CurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var vm = new ResidentDashboardViewModel
        {
            FullName = user.FullName,
            EmailAlerts = user.EmailAlerts,
            MemberSince = user.CreatedAt,
            Weather = await _weather.GetAsync(),
            Advisories = await _db.Advisories.Active(DateTime.Now)
                .OrderByDescending(a => a.PublishedAt).Take(3).ToListAsync(),
            LocationCount = await _db.EvacuationCenters.CountAsync()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEmailAlerts(bool enabled)
    {
        var user = await CurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        user.EmailAlerts = enabled;
        await _db.SaveChangesAsync();
        TempData["Message"] = enabled ? "Email alerts are on." : "Email alerts are off.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<User?> CurrentUserAsync()
    {
        var idText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idText, out var id) ? await _db.Users.FindAsync(id) : null;
    }
}
