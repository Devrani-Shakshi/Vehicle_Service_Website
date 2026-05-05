using System.ComponentModel.DataAnnotations;

namespace ServicePlatform.Models;

public class ActivityLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty; // e.g., "Role Change", "Payment Override"

    [Required]
    public string Details { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
    public string PerformedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
