using CommunityWeather.Data;
using CommunityWeather.Models;
using CommunityWeather.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Controllers;

public class HomeController : Controller
{
    private readonly WeatherService _weather;
    private readonly AppDbContext _db;

    public HomeController(WeatherService weather, AppDbContext db)
    {
        _weather = weather;
        _db = db;
    }

    public async Task<IActionResult> Index(double? lat, double? lon)
    {
        var vm = new HomeViewModel
        {
            Weather = await _weather.GetAsync(lat, lon),
            Advisories = await _db.Advisories.Active(DateTime.Now)
                .OrderByDescending(a => a.PublishedAt).Take(3).ToListAsync()
        };
        return View(vm);
    }

    public IActionResult Error() => View();
}
