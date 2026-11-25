namespace CommunityWeather.Models;

public static class Lookups
{
    public static readonly string[] Hazards = { "Typhoon", "Flood", "Earthquake", "Fire", "Landslide", "Other" };
    public static readonly string[] Categories = { "Weather", "Evacuation", "Health", "Road/Traffic", "General" };
    public static readonly string[] Priorities = { "Normal", "High" };
    public static readonly string[] Statuses = { "Draft", "Published", "Archived" };
    public static readonly string[] FacilityTypes =
        { "Evacuation Center", "Hospital", "Fire Station", "Police Station", "Barangay Hall" };
}
