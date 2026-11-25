using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWeather.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    /// <summary>Stored trimmed and lower-case so duplicates cannot differ by letter case.</summary>
    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    /// <summary>PBKDF2 hash with a per-user salt. The plain password is never stored.</summary>
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = "";

    [Required, MaxLength(20)]
    public string Role { get; set; } = Roles.Resident;

    public bool IsActive { get; set; } = true;

    public bool EmailAlerts { get; set; } = true;

    public int FailedLoginAttempts { get; set; }

    /// <summary>UTC time until which sign-in is blocked after too many failed attempts.</summary>
    public DateTime? LockoutEnd { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [NotMapped]
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}
