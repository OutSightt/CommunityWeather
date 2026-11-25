using CommunityWeather.Data;
using CommunityWeather.Models;
using CommunityWeather.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityWeather.Controllers;

public class CentersController : Controller
{
    private const string Fields = "Name,Address,ContactNumber,OperationalNotes,FacilityType,Latitude,Longitude";
    private readonly AppDbContext _db;
    private readonly GeocodingService _geo;
    private readonly GoogleMapsSettings _maps;

    public CentersController(AppDbContext db, GeocodingService geo, IOptions<GoogleMapsSettings> maps)
    {
        _db = db;
        _geo = geo;
        _maps = maps.Value;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.MapsKey = _maps.ApiKey;
        return View(await _db.EvacuationCenters.OrderBy(c => c.FacilityType).ThenBy(c => c.Name).ToListAsync());
    }

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Manage() =>
        View(await _db.EvacuationCenters.OrderBy(c => c.Name).ToListAsync());

    [Authorize(Roles = Roles.Admin), HttpGet]
    public IActionResult Create() => View(new EvacuationCenter());

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Fields)] EvacuationCenter model)
    {
        if (!ModelState.IsValid) return View(model);
        await FillCoordinatesAsync(model);
        model.UpdatedAt = DateTime.Now;
        _db.EvacuationCenters.Add(model);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Location saved.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var center = await _db.EvacuationCenters.FindAsync(id);
        return center == null ? NotFound() : View(center);
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind(Fields)] EvacuationCenter model)
    {
        var center = await _db.EvacuationCenters.FindAsync(id);
        if (center == null) return NotFound();
        if (!ModelState.IsValid) { model.Id = id; return View(model); }

        await FillCoordinatesAsync(model);
        center.Name = model.Name;
        center.Address = model.Address;
        center.ContactNumber = model.ContactNumber;
        center.OperationalNotes = model.OperationalNotes;
        center.FacilityType = model.FacilityType;
        center.Latitude = model.Latitude;
        center.Longitude = model.Longitude;
        center.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        TempData["Message"] = "Location updated.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var center = await _db.EvacuationCenters.FindAsync(id);
        if (center != null)
        {
            _db.EvacuationCenters.Remove(center);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Location deleted.";
        }
        return RedirectToAction(nameof(Manage));
    }

    /// <summary>If the admin left latitude/longitude empty, look them up from the address with Google Geocoding.</summary>
    private async Task FillCoordinatesAsync(EvacuationCenter model)
    {
        if (model.Latitude.HasValue && model.Longitude.HasValue) return;

        var result = await _geo.GeocodeAsync(model.Address);
        if (result is { } coords)
        {
            model.Latitude = coords.Lat;
            model.Longitude = coords.Lng;
        }
        else
        {
            TempData["Warning"] = "Coordinates could not be found automatically, so this location will not appear on the map. " +
                                  "Check the address, or enter latitude and longitude manually.";
        }
    }
}
