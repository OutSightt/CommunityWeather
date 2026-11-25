using System.Globalization;
using System.Text.Json;
using CommunityWeather.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CommunityWeather.Services;

/// <summary>External system 1: OpenWeather API (current weather + 5-day/3-hour forecast).</summary>
public class WeatherService
{
    private const string CacheKey = "weather-vm";
    private readonly HttpClient _http;
    private readonly WeatherSettings _settings;
    private readonly IMemoryCache _cache;

    public WeatherService(HttpClient http, IOptions<WeatherSettings> settings, IMemoryCache cache)
    {
        _http = http;
        _settings = settings.Value;
        _cache = cache;
    }

    /// <summary>Weather for the configured community, or for the visitor's own coordinates when supplied.</summary>
    public async Task<WeatherViewModel> GetAsync(double? lat = null, double? lon = null)
    {
        var custom = lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
        var useLat = custom ? lat!.Value : _settings.Latitude;
        var useLon = custom ? lon!.Value : _settings.Longitude;
        var inv0 = CultureInfo.InvariantCulture;
        var cacheKey = custom ? $"weather-{Math.Round(useLat, 2).ToString(inv0)}-{Math.Round(useLon, 2).ToString(inv0)}" : CacheKey;

        if (_cache.TryGetValue(cacheKey, out WeatherViewModel? cached) && cached != null)
            return cached;

        var vm = new WeatherViewModel
        {
            Location = custom ? "Your location" : _settings.LocationName,
            Units = _settings.Units,
            IsCustomLocation = custom
        };

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            vm.Error = "OpenWeather API key is not configured. Add it to appsettings.json (Weather:ApiKey).";
            return vm;
        }

        try
        {
            var inv = CultureInfo.InvariantCulture;
            var query = $"lat={useLat.ToString(inv)}&lon={useLon.ToString(inv)}" +
                        $"&units={_settings.Units}&appid={_settings.ApiKey}";

            var currentJson = await _http.GetStringAsync($"https://api.openweathermap.org/data/2.5/weather?{query}");
            using (var doc = JsonDocument.Parse(currentJson))
            {
                var root = doc.RootElement;
                if (custom && root.TryGetProperty("name", out var placeName) && !string.IsNullOrWhiteSpace(placeName.GetString()))
                    vm.Location = placeName.GetString()!;
                var main = root.GetProperty("main");
                vm.Temperature = main.GetProperty("temp").GetDouble();
                vm.FeelsLike = main.GetProperty("feels_like").GetDouble();
                vm.Humidity = main.GetProperty("humidity").GetInt32();
                var w = root.GetProperty("weather")[0];
                vm.Description = w.GetProperty("description").GetString() ?? "";
                vm.Icon = w.GetProperty("icon").GetString() ?? "";
                if (root.TryGetProperty("wind", out var wind) && wind.TryGetProperty("speed", out var ws))
                    vm.WindSpeed = ws.GetDouble();
                if (root.TryGetProperty("rain", out var rain) && rain.TryGetProperty("1h", out var r1))
                    vm.RainLastHourMm = r1.GetDouble();
            }

            var forecastJson = await _http.GetStringAsync($"https://api.openweathermap.org/data/2.5/forecast?{query}");
            using (var doc = JsonDocument.Parse(forecastJson))
            {
                var slots = new List<(DateTime Date, double Temp, string Desc, string Icon, double Pop, double Rain)>();
                foreach (var item in doc.RootElement.GetProperty("list").EnumerateArray())
                {
                    var date = DateTime.ParseExact(item.GetProperty("dt_txt").GetString()!,
                        "yyyy-MM-dd HH:mm:ss", inv).Date;
                    var w = item.GetProperty("weather")[0];
                    var pop = item.TryGetProperty("pop", out var p) ? p.GetDouble() : 0;
                    double rain = 0;
                    if (item.TryGetProperty("rain", out var r) && r.TryGetProperty("3h", out var r3))
                        rain = r3.GetDouble();
                    slots.Add((date, item.GetProperty("main").GetProperty("temp").GetDouble(),
                        w.GetProperty("description").GetString() ?? "", w.GetProperty("icon").GetString() ?? "",
                        pop, rain));
                }

                vm.Forecast = slots
                    .GroupBy(s => s.Date)
                    .OrderBy(g => g.Key)
                    .Take(5)
                    .Select(g =>
                    {
                        var top = g.GroupBy(s => s.Desc).OrderByDescending(x => x.Count()).First();
                        return new ForecastDay
                        {
                            Date = g.Key,
                            MinTemp = g.Min(s => s.Temp),
                            MaxTemp = g.Max(s => s.Temp),
                            Description = top.Key,
                            Icon = top.First().Icon,
                            RainChancePercent = Math.Round(g.Max(s => s.Pop) * 100),
                            RainMm = Math.Round(g.Sum(s => s.Rain), 1)
                        };
                    })
                    .ToList();
            }

            vm.Ok = true;
            vm.RetrievedAt = DateTime.Now;
            _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(10));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            vm.Ok = false;
            vm.Error = "OpenWeather rejected the API key. A new key can take up to a couple of hours to activate. If it still fails later, copy the key again from your OpenWeather account.";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException)
        {
            vm.Ok = false;
            vm.Error = "Weather data could not be retrieved right now. Check the API key and internet connection.";
        }

        return vm;
    }
}
