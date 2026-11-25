using System.ComponentModel.DataAnnotations;

namespace CommunityWeather.Models;

public static class PasswordRules
{
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$";
    public const string Message = "Use at least 8 characters with an uppercase letter, a lowercase letter and a number.";
}

public class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength = 2), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Display(Name = "Send me email alerts for high-priority advisories")]
    public bool EmailAlerts { get; set; } = true;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    [Required, MaxLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    [Display(Name = "Send me email alerts for high-priority advisories")]
    public bool EmailAlerts { get; set; }
}

public class ForecastDay
{
    public DateTime Date { get; set; }
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public double RainChancePercent { get; set; }
    public double RainMm { get; set; }
}

public class WeatherViewModel
{
    public bool Ok { get; set; }
    public bool IsCustomLocation { get; set; }
    public string? Error { get; set; }
    public string Location { get; set; } = "";
    public string Units { get; set; } = "metric";
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public double? RainLastHourMm { get; set; }
    public DateTime RetrievedAt { get; set; }
    public List<ForecastDay> Forecast { get; set; } = new();

    public string TempUnit => Units == "imperial" ? "°F" : Units == "standard" ? "K" : "°C";
    public string WindUnit => Units == "imperial" ? "mph" : "m/s";
}

public class HomeViewModel
{
    public WeatherViewModel Weather { get; set; } = new();
    public List<Advisory> Advisories { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public int Admins { get; set; }
    public int Residents { get; set; }
    public int Subscribers { get; set; }
    public int ActiveAdvisories { get; set; }
    public int Drafts { get; set; }
    public int EmailPending { get; set; }
    public int Locations { get; set; }
    public int Unmapped { get; set; }
    public int Guides { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";

    [Required, RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message), DataType(DataType.Password), Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The new passwords do not match."), Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = "";
}

public class ResidentDashboardViewModel
{
    public string FullName { get; set; } = "";
    public bool EmailAlerts { get; set; }
    public DateTime MemberSince { get; set; }
    public WeatherViewModel Weather { get; set; } = new();
    public List<Advisory> Advisories { get; set; } = new();
    public int LocationCount { get; set; }
}
