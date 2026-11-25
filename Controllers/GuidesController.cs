using CommunityWeather.Data;
using CommunityWeather.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Controllers;

public class GuidesController : Controller
{
    private const string Fields = "Title,Hazard,Content";
    private readonly AppDbContext _db;

    public GuidesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? hazard)
    {
        var query = _db.PreparednessGuides.AsQueryable();
        if (!string.IsNullOrWhiteSpace(hazard))
            query = query.Where(g => g.Hazard == hazard);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(g => g.Title.Contains(q) || g.Content.Contains(q));

        ViewBag.Q = q;
        ViewBag.Hazard = hazard;
        return View(await query.OrderBy(g => g.Hazard).ThenBy(g => g.Title).ToListAsync());
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Manage() =>
        View(await _db.PreparednessGuides.OrderBy(g => g.Hazard).ThenBy(g => g.Title).ToListAsync());

    [Authorize(Roles = Roles.Admin), HttpGet]
    public IActionResult Create() => View(new PreparednessGuide());

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Fields)] PreparednessGuide model)
    {
        if (!ModelState.IsValid) return View(model);
        model.UpdatedAt = DateTime.Now;
        _db.PreparednessGuides.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Guide saved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var guide = await _db.PreparednessGuides.FindAsync(id);
        return guide == null ? NotFound() : View(guide);
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] PreparednessGuide model)
    {
        var guide = await _db.PreparednessGuides.FindAsync(id);
        if (guide == null) return NotFound();
        if (!ModelState.IsValid) { model.Id = id; return View(model); }

        guide.Title = model.Title;
        guide.Hazard = model.Hazard;
        guide.Content = model.Content;
        guide.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Guide updated.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var guide = await _db.PreparednessGuides.FindAsync(id);
        if (guide != null)
        {
            _db.PreparednessGuides.Remove(guide);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Guide deleted.";
        }
        return RedirectToAction(nameof(Manage));
    }
}
