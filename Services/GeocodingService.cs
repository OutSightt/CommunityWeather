using System.Globalization;
using System.Text.Json;
using CommunityWeather.Models;
using Microsoft.Extensions.Options;

namespace CommunityWeather.Services;

/// <summary>
/// Converts an address to coordinates. Uses the Google Geocoding API when a Google Maps key is configured.
/// Without a key (or if Google cannot find the address) it falls back to OpenStreetMap Nominatim, which is free.
/// </summary>
public class GeocodingService
{
    private readonly HttpClient _http;
    private readonly GoogleMapsSettings _settings;

    public GeocodingService(HttpClient http, IOptions<GoogleMapsSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<(double Lat, double Lng)?> GeocodeAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            var google = await GeocodeWithGoogleAsync(address);
            if (google != null) return google;
        }
        return await GeocodeWithNominatimAsync(address);
    }

    private async Task<(double Lat, double Lng)?> GeocodeWithGoogleAsync(string address)
    {
        try
        {
            var url = "https://maps.googleapis.com/maps/api/geocode/json?address=" +
                      Uri.EscapeDataString(address) + "&key=" + _settings.ApiKey;
            using var doc = JsonDocument.Parse(await _http.GetStringAsync(url));
            var root = doc.RootElement;
            if (root.GetProperty("status").GetString() != "OK") return null;

            var loc = root.GetProperty("results")[0].GetProperty("geometry").GetProperty("location");
            return (loc.GetProperty("lat").GetDouble(), loc.GetProperty("lng").GetDouble());
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<(double Lat, double Lng)?> GeocodeWithNominatimAsync(string address)
    {
        try
        {
            var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=ph&q=" +
                      Uri.EscapeDataString(address);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Nominatim's usage policy requires an identifying User-Agent.
            request.Headers.UserAgent.ParseAdd("CommunityWeatherStudentProject/1.0");

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var results = doc.RootElement;
            if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0) return null;

            var first = results[0];
            var lat = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
            var lon = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
            return (lat, lon);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException
                                       or TaskCanceledException or FormatException)
        {
            return null;
        }
    }
}
