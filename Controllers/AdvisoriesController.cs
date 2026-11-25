using CommunityWeather.Data;
using CommunityWeather.Models;
using CommunityWeather.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Controllers;

public class AdvisoriesController : Controller
{
    private const string Fields = "Title,Message,AffectedArea,Category,Priority,Status,ValidUntil";
    private readonly AppDbContext _db;
    private readonly EmailService _email;

    public AdvisoriesController(AppDbContext db, EmailService email)
    {
        _db = db;
        _email = email;
    }

    // ---------- Public pages ----------

    public async Task<IActionResult> Index() =>
        View(await _db.Advisories.Active(DateTime.Now).OrderByDescending(a => a.PublishedAt).ToListAsync());

    public async Task<IActionResult> Archive() =>
        View(await _db.Advisories.Archived(DateTime.Now).OrderByDescending(a => a.PublishedAt).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var advisory = await _db.Advisories.FindAsync(id);
        if (advisory == null) return NotFound();
        if (advisory.Status == "Draft" && !User.IsInRole(Roles.Admin)) return NotFound();
        return View(advisory);
    }

    // ---------- Administrator pages ----------

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Manage() =>
        View(await _db.Advisories.OrderByDescending(a => a.CreatedAt).ToListAsync());

    [Authorize(Roles = Roles.Admin), HttpGet]
    public IActionResult Create() => View(new Advisory { ValidUntil = DateTime.Now.AddDays(1) });

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Fields)] Advisory model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedAt = DateTime.Now;
        _db.Advisories.Add(model);
        await SaveAndNotifyAsync(model);
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var advisory = await _db.Advisories.FindAsync(id);
        return advisory == null ? NotFound() : View(advisory);
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] Advisory model)
    {
        var advisory = await _db.Advisories.FindAsync(id);
        if (advisory == null) return NotFound();
        if (!ModelState.IsValid) { model.Id = id; return View(model); }

        advisory.Title = model.Title;
        advisory.Message = model.Message;
        advisory.AffectedArea = model.AffectedArea;
        advisory.Category = model.Category;
        advisory.Priority = model.Priority;
        advisory.Status = model.Status;
        advisory.ValidUntil = model.ValidUntil;
        await SaveAndNotifyAsync(advisory);
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var advisory = await _db.Advisories.FindAsync(id);
        if (advisory == null) return NotFound();
        advisory.Status = "Published";
        await SaveAndNotifyAsync(advisory);
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveNotice(int id)
    {
        var advisory = await _db.Advisories.FindAsync(id);
        if (advisory == null) return NotFound();
        advisory.Status = "Archived";
        await _db.SaveChangesAsync();
        TempData["Message"] = "Advisory archived.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var advisory = await _db.Advisories.FindAsync(id);
        if (advisory != null)
        {
            _db.Advisories.Remove(advisory);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Advisory deleted.";
        }
        return RedirectToAction(nameof(Manage));
    }

    /// <summary>Saves the advisory, stamps the publication time, and emails subscribers for high-priority notices.</summary>
    private async Task SaveAndNotifyAsync(Advisory advisory)
    {
        if (advisory.Status == "Published" && advisory.PublishedAt == null)
            advisory.PublishedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        if (advisory.Status == "Published" && advisory.Priority == "High" && !advisory.EmailSent)
        {
            var recipients = (await _db.Users.Where(u => u.EmailAlerts && u.IsActive)
                    .Select(u => new { u.Email, u.FullName }).ToListAsync())
                .Select(u => (u.Email, u.FullName)).ToList();

            if (await _email.SendAdvisoryAsync(advisory, recipients))
            {
                advisory.EmailSent = true;
                await _db.SaveChangesAsync();
                TempData["Message"] = $"Advisory published. Email alert sent to {recipients.Count} subscriber(s).";
            }
            else
            {
                TempData["Message"] = "Advisory published, but the email alert was not sent. Check the SendGrid settings.";
            }
        }
        else
        {
            TempData["Message"] = "Advisory saved.";
        }
    }
}
