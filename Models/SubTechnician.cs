using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public class SubTechnician
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? Mobile { get; set; }

    [MaxLength(200)]
    public string? Specialization { get; set; } // e.g. "Battery", "Motor", "Software"

    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Owner (ServiceProvider / Workshop)
    [Required]
    public string ProviderId { get; set; } = string.Empty;

    [ForeignKey("ProviderId")]
    public virtual ApplicationUser Provider { get; set; } = null!;
}
