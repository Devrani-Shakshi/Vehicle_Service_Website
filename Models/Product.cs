using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServicePlatform.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Range(0, 1000000, ErrorMessage = "Price must be between 0 and 1,000,000")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Range(0, 1000000, ErrorMessage = "Discount Price must be between 0 and 1,000,000")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Range(0, 10000, ErrorMessage = "Stock cannot be negative")]
    public int StockQuantity { get; set; } = 0;

    [MaxLength(50)]
    public string? SKU { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(2000)]
    public string? CompatibleVehicleModels { get; set; } // e.g. "Ather 450X, Ola S1 Pro"
    
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Foreign key - Shopkeeper
    [Required]
    public string ShopkeeperId { get; set; } = string.Empty;

    [ForeignKey("ShopkeeperId")]
    public virtual ApplicationUser Shopkeeper { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
