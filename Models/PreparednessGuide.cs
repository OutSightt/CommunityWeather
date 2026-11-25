using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWeather.Models;

public class PreparednessGuide
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required, MaxLength(50)]
    public string Hazard { get; set; } = "Typhoon";

    [Required, Column(TypeName = "text")]
    public string Content { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
