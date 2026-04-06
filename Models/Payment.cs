using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}

public enum PaymentGateway
{
    Razorpay,
    Stripe
}

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "INR";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentGateway Gateway { get; set; }

    [MaxLength(200)]
    public string? TransactionId { get; set; }

    [MaxLength(200)]
    public string? GatewayOrderId { get; set; }

    [MaxLength(200)]
    public string? GatewayPaymentId { get; set; }

    [MaxLength(500)]
    public string? GatewaySignature { get; set; }

    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Foreign keys
    [Required]
    public string UserId { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public int? ServiceRequestId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ServiceRequestId")]
    public virtual ServiceRequest? ServiceRequest { get; set; }
}
