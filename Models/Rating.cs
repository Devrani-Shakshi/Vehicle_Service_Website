using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public class Rating
{
    [Key]
    public int Id { get; set; }

    [Range(1, 5)]
    public int Stars { get; set; }

    [MaxLength(1000)]
    public string? ReviewText { get; set; }
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Foreign keys
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ServiceProviderId { get; set; } = string.Empty;

    public int? ServiceRequestId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey("ServiceProviderId")]
    public virtual ApplicationUser ServiceProvider { get; set; } = null!;

    [ForeignKey("ServiceRequestId")]
    public virtual ServiceRequest? ServiceRequest { get; set; }
}
