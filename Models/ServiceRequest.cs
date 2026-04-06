using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public enum ServiceRequestType
{
    General,
    Urgent,
    Breakdown
}

public enum ServiceRequestStatus
{
    Pending,
    Accepted,
    InProgress,
    Completed,
    Rejected,
    Cancelled
}

public class ServiceRequest
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public ServiceRequestType RequestType { get; set; } = ServiceRequestType.General;
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    [MaxLength(100)]
    public string? Category { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [MaxLength(500)]
    public string? LocationAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Foreign keys
    [Required]
    public string UserId { get; set; } = string.Empty;
    public string? ServiceProviderId { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey("ServiceProviderId")]
    public virtual ApplicationUser? ServiceProvider { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public virtual Payment? Payment { get; set; }
}
