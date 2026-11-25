using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommunityWeather.Models;

public class EvacuationCenter
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [Required, MaxLength(255)]
    public string Address { get; set; } = "";

    [MaxLength(100), Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = "";

    [Column(TypeName = "text"), Display(Name = "Operational notes")]
    public string OperationalNotes { get; set; } = "";

    [Required, MaxLength(50), Display(Name = "Facility type")]
    public string FacilityType { get; set; } = "Evacuation Center";

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
