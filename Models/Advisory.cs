using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWeather.Models;

public class Advisory
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required, Column(TypeName = "text")]
    public string Message { get; set; } = "";

    [Required, MaxLength(150), Display(Name = "Affected area")]
    public string AffectedArea { get; set; } = "";

    [Required, MaxLength(50)]
    public string Category { get; set; } = "General";

    [Required, MaxLength(10)]
    public string Priority { get; set; } = "Normal"; // Normal | High

    [Required, MaxLength(15)]
    public string Status { get; set; } = "Draft"; // Draft | Published | Archived

    [Display(Name = "Published at")]
    public DateTime? PublishedAt { get; set; }

    [Display(Name = "Valid until")]
    public DateTime? ValidUntil { get; set; }

    public bool EmailSent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
