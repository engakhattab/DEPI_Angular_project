using System.ComponentModel.DataAnnotations;

namespace HR.DTOs.Transportation;

public class TripCreateRequest
{
    [Required]
    [StringLength(200)]
    public string ReferenceName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Project { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Route { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TripType { get; set; } = string.Empty;

    [Required]
    public DateOnly TripDate { get; set; }
}
