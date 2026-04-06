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
    Task<Order?> GetByIdAsync(int id);
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task<int> GetOrderCountAsync(OrderStatus? status = null, string? userId = null);
}

public interface IPaymentService
{
    Task<Payment> CreatePaymentAsync(string userId, decimal amount, PaymentGateway gateway, int? orderId = null, int? serviceRequestId = null);
    Task<bool> CompletePaymentAsync(int paymentId, string transactionId, string? gatewayPaymentId = null, string? signature = null);
    Task<bool> FailPaymentAsync(int paymentId, string reason);
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId);
    Task<IEnumerable<Payment>> GetAllPaymentsAsync();
    Task<decimal> GetTotalRevenueAsync();
    Task<decimal> GetMonthlyRevenueAsync();
    Task<Payment?> GetByIdAsync(int id);
}
