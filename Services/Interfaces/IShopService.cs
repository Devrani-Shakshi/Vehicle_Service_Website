using ServicePlatform.Models;
using ServicePlatform.ViewModels.Shop;

namespace ServicePlatform.Services.Interfaces;

public interface IProductService
{
    Task<Product> CreateProductAsync(ProductViewModel model, string shopkeeperId);
    Task<bool> UpdateProductAsync(int id, ProductViewModel model, string shopkeeperId);
    Task<bool> DeleteProductAsync(int id, string shopkeeperId);
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetShopkeeperProductsAsync(string shopkeeperId);
    Task<ShopCatalogViewModel> GetCatalogAsync(string? search, string? category, string? sortBy, int page, int pageSize);
    Task<List<string>> GetCategoriesAsync();
    Task<bool> UpdateStockAsync(int id, int quantity, string shopkeeperId);
}

public interface ICartService
{
    Task<bool> AddToCartAsync(string userId, int productId, int quantity = 1);
    Task<bool> UpdateQuantityAsync(int cartItemId, int quantity);
    Task<bool> RemoveFromCartAsync(int cartItemId);
    Task<CartViewModel> GetCartAsync(string userId);
    Task<int> GetCartCountAsync(string userId);
    Task ClearCartAsync(string userId);
}

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string userId, string shippingAddress, string? notes);
    Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
    Task<IEnumerable<Order>> GetShopkeeperOrdersAsync(string shopkeeperId);
    Task<Order?> GetByIdAsync(int id, string? userId = null);
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);
    Task<IEnumerable<Order>> GetAllOrdersAsync(int page = 1, int pageSize = 10);
    Task<int> GetOrderCountAsync(OrderStatus? status = null, string? userId = null);
    Task<bool> UpdateShippingInfoAsync(int id, string trackingNumber, string carrier, OrderStatus newStatus);
}

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(string userId, decimal amount, PaymentGateway gateway, int? orderId = null, int? serviceRequestId = null);
    Task<bool> UpdateGatewayOrderIdAsync(int paymentId, string gatewayOrderId);
    Task<bool> CompletePaymentAsync(int paymentId, string transactionId, string? gatewayPaymentId = null, string? signature = null);
    Task<string> InitializeRazorpayOrderAsync(int paymentId); 
    Task<string> InitializeStripeSessionAsync(int paymentId, string successUrl, string cancelUrl);
    Task<bool> FailPaymentAsync(int paymentId, string reason);
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId);
    Task<IEnumerable<Payment>> GetAllPaymentsAsync(int page = 1, int pageSize = 10);
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetMonthlyRevenueAsync();
    Task<Payment?> GetByIdAsync(int id);
}
