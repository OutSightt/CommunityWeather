using System.Security.Claims;
using CommunityWeather.Data;
using CommunityWeather.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Controllers;

/// <summary>Administrator dashboard and user management. Residents are not allowed here.</summary>
[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var vm = new AdminDashboardViewModel
        {
            Admins = await _db.Users.CountAsync(u => u.Role == Roles.Admin),
            Residents = await _db.Users.CountAsync(u => u.Role == Roles.Resident),
            Subscribers = await _db.Users.CountAsync(u => u.EmailAlerts),
            ActiveAdvisories = await _db.Advisories.Active(now).CountAsync(),
            Drafts = await _db.Advisories.CountAsync(a => a.Status == "Draft"),
            EmailPending = await _db.Advisories.CountAsync(a => a.Status == "Published" && a.Priority == "High" && !a.EmailSent),
            Locations = await _db.EvacuationCenters.CountAsync(),
            Unmapped = await _db.EvacuationCenters.CountAsync(c => c.Latitude == null || c.Longitude == null),
            Guides = await _db.PreparednessGuides.CountAsync()
        };
        return View(vm);
    }

    // ---------- User management ----------

    public async Task<IActionResult> Users(string? q, string? role)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(role) && Roles.All.Contains(role))
            query = query.Where(u => u.Role == role);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }

        ViewBag.Q = q;
        ViewBag.Role = role;
        ViewBag.CurrentUserId = CurrentUserId();
        return View(await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        if (!Roles.All.Contains(role)) return BadRequest();
        if (id == CurrentUserId())
        {
            TempData["Warning"] = "You cannot change your own role. Ask another administrator.";
            return RedirectToAction(nameof(Users));
        }

        var target = await _db.Users.FindAsync(id);
        if (target == null) return NotFound();

        target.Role = role;
        await _db.SaveChangesAsync();
        TempData["Message"] = $"{target.FullName} is now a {(role == Roles.Admin ? "community administrator" : "resident")}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        if (id == CurrentUserId())
        {
            TempData["Warning"] = "You cannot disable your own account.";
            return RedirectToAction(nameof(Users));
        }

        var target = await _db.Users.FindAsync(id);
        if (target == null) return NotFound();

        target.IsActive = active;
        if (active)
        {
            target.FailedLoginAttempts = 0;
            target.LockoutEnd = null;
        }
        await _db.SaveChangesAsync();
        TempData["Message"] = active ? $"{target.FullName}'s account is enabled." : $"{target.FullName}'s account is disabled and signed out.";
        return RedirectToAction(nameof(Users));
    }

    private int CurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
}
