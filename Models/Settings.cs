namespace CommunityWeather.Models;

public class WeatherSettings
{
    public string ApiKey { get; set; } = "";
    public string LocationName { get; set; } = "Community";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Units { get; set; } = "metric";
}

public class GoogleMapsSettings
{
    public string ApiKey { get; set; } = "";
}

public class EmailSettings
{
    public string SendGridApiKey { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Community Weather Alerts";
}

/// <summary>Creates the first administrator account in the database on first run, if none exists.</summary>
public class SeedAdminSettings
{
    public string FullName { get; set; } = "Community Administrator";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
