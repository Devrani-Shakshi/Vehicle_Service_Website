using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public class Commission
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int Month { get; set; }
    public int Year { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalIncome { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; } // Configurable % of TotalIncome (set in appsettings.json)

    [Column(TypeName = "decimal(5,2)")]
    public decimal AppliedPercentage { get; set; } // Historical rate at the time of calculation
    public string Role { get; set; } = string.Empty;

    public bool IsPaid { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
