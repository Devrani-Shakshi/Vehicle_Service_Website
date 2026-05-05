using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public enum TicketStatus { Open, InProgress, Resolved, Closed }
public enum TicketCategory { General, Payment, ProductReturn, ServiceComplaint }

public class SupportTicket
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketCategory Category { get; set; } = TicketCategory.General;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // Relationships
    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    public string? OrderId { get; set; } // For product returns
    public int? ServiceRequestId { get; set; } // For service complaints

    public string? AdminResponse { get; set; }
}
