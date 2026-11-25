namespace CommunityWeather.Models;

/// <summary>The two user roles defined in the SRS.</summary>
public static class Roles
{
    public const string Admin = "Admin";         // Community Administrator
    public const string Resident = "Resident";
    public static readonly string[] All = { Admin, Resident };
}
