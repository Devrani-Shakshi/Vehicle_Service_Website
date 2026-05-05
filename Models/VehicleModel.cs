using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public enum VehicleType
{
    Scooter,
    Bike,
    Car,
    SUV,
    Commercial
}

public class VehicleModel
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public VehicleType Type { get; set; }

    [Required, MaxLength(50)]
    public string ModelNumber { get; set; } = string.Empty;

    public int ReleaseYear { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key - Service Provider (The OEM/Company)
    [Required]
    public string ServiceProviderId { get; set; } = string.Empty;

    [ForeignKey("ServiceProviderId")]
    public virtual ApplicationUser ServiceProvider { get; set; } = null!;

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
