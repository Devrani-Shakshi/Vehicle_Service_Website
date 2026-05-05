using ServicePlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace ServicePlatform.ViewModels.Shop;

public class ProductViewModel
{
    [Required(ErrorMessage = "Product name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [MaxLength(50)]
    public string? SKU { get; set; }
}

public class ShopkeeperDashboardViewModel
{
    public string ShopkeeperName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    
    public List<RevenueStat> RevenueTrend { get; set; } = new();
    public List<CategoryStat> CategoryDistribution { get; set; } = new();

    public List<Product> RecentProducts { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
    public List<Payment> RecentPayments { get; set; } = new();
}

public class RevenueStat { public string Month { get; set; } = ""; public decimal Amount { get; set; } }
public class CategoryStat { public string Category { get; set; } = ""; public int Count { get; set; } }

public class ShipmentUpdateViewModel
{
    public int OrderId { get; set; }
    [Required] public string TrackingNumber { get; set; } = "";
    [Required] public string Carrier { get; set; } = "";
    public OrderStatus NewStatus { get; set; } = OrderStatus.Shipped;
}

public class CartViewModel
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal => Items.Sum(i => i.TotalPrice);
    public decimal Tax => SubTotal * 0.18m; // 18% GST
    public decimal Total => SubTotal + Tax;
}

public class CartItemDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}

public class CheckoutViewModel
{
    public CartViewModel Cart { get; set; } = new();

    [Required(ErrorMessage = "Shipping address is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 500 characters")]
    public string ShippingAddress { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Please select a payment gateway")]
    public string PaymentGateway { get; set; } = "Stripe";
}

public class ShopCatalogViewModel
{
    public List<Product> Products { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 12;
    public List<string> Categories { get; set; } = new();
}
