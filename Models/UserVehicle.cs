using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public class UserVehicle
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Make { get; set; } = string.Empty; // e.g. "Tata"

    [Required, MaxLength(100)]
    public string Model { get; set; } = string.Empty; // e.g. "Nexon EV"

    [Range(2015, 2035)]
    public int Year { get; set; }

    [Range(1, 200)]
    public int BatteryCapacityKWh { get; set; } // e.g. 30

    [MaxLength(20)]
    public string? RegistrationNumber { get; set; }

    public bool IsPrimary { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
