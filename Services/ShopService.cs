using Microsoft.EntityFrameworkCore;
using ServicePlatform.Models;
using ServicePlatform.Repositories.Interfaces;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Shop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace ServicePlatform.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _repository;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    public ProductService(IGenericRepository<Product> repository, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Product> CreateProductAsync(ProductViewModel model, string shopkeeperId)
    {
        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            DiscountPrice = model.DiscountPrice,
            Category = model.Category,
            ImageUrl = model.ImageUrl,
            StockQuantity = model.StockQuantity,
            SKU = model.SKU,
            ShopkeeperId = shopkeeperId,
            IsActive = true
        };

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateProductAsync(int id, ProductViewModel model, string shopkeeperId)
    {
        var product = await _repository.FirstOrDefaultAsync(p => p.Id == id && p.ShopkeeperId == shopkeeperId);
        if (product == null) return false;

        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.DiscountPrice = model.DiscountPrice;
        product.Category = model.Category;
        product.ImageUrl = model.ImageUrl;
        product.StockQuantity = model.StockQuantity;
        product.SKU = model.SKU;
        product.UpdatedAt = DateTime.UtcNow;

        _repository.Update(product);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProductAsync(int id, string shopkeeperId)
    {
        var product = await _repository.FirstOrDefaultAsync(p => p.Id == id && p.ShopkeeperId == shopkeeperId);
        if (product == null) return false;

        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        _repository.Update(product);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Product?> GetByIdAsync(int id) =>
        await _repository.Query().Include(p => p.Shopkeeper).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Product>> GetShopkeeperProductsAsync(string shopkeeperId) =>
        await _repository.Query()
            .Where(p => p.ShopkeeperId == shopkeeperId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<ShopCatalogViewModel> GetCatalogAsync(string? search, string? category, string? sortBy, int page, int pageSize)
    {
        string cacheKey = $"catalog_{search}_{category}_{sortBy}_{page}_{pageSize}";
        
        if (_cache.TryGetValue<ShopCatalogViewModel>(cacheKey, out var cachedResult))
        {
            return cachedResult!;
        }

        var query = _repository.Query().Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var categories = await _repository.Query().Where(p => p.IsActive && p.Category != null).Select(p => p.Category!).Distinct().ToListAsync();

        var result = new ShopCatalogViewModel
        {
            Products = products,
            SearchTerm = search,
            Category = category,
            SortBy = sortBy,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            Categories = categories
        };

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

        return result;
    }

    public async Task<List<string>> GetCategoriesAsync() =>
        await _repository.Query()
            .Where(p => p.IsActive && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .ToListAsync();

    public async Task<bool> UpdateStockAsync(int id, int quantity, string shopkeeperId)
    {
        var product = await _repository.FirstOrDefaultAsync(p => p.Id == id && p.ShopkeeperId == shopkeeperId);
        if (product == null) return false;

        product.StockQuantity = quantity;
        product.UpdatedAt = DateTime.UtcNow;
        _repository.Update(product);
        await _repository.SaveChangesAsync();
        return true;
    }
}

public class CartService : ICartService
{
    private readonly IGenericRepository<CartItem> _cartRepository;
    private readonly IGenericRepository<Product> _productRepository;

    public CartService(IGenericRepository<CartItem> cartRepository, IGenericRepository<Product> productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<bool> AddToCartAsync(string userId, int productId, int quantity = 1)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null || !product.IsActive) return false;

        var existingItem = await _cartRepository.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            _cartRepository.Update(existingItem);
        }
        else
        {
            await _cartRepository.AddAsync(new CartItem { UserId = userId, ProductId = productId, Quantity = quantity });
        }

        await _cartRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateQuantityAsync(int cartItemId, int quantity)
    {
        var item = await _cartRepository.GetByIdAsync(cartItemId);
        if (item == null) return false;

        if (quantity <= 0)
        {
            _cartRepository.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
            _cartRepository.Update(item);
        }

        await _cartRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFromCartAsync(int cartItemId)
    {
        var item = await _cartRepository.GetByIdAsync(cartItemId);
        if (item == null) return false;

        _cartRepository.Remove(item);
        await _cartRepository.SaveChangesAsync();
        return true;
    }

    public async Task<CartViewModel> GetCartAsync(string userId)
    {
        var items = await _cartRepository.Query()
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return new CartViewModel
        {
            Items = items.Select(c => new CartItemDto
            {
                CartItemId = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product.Name,
                ImageUrl = c.Product.ImageUrl,
                UnitPrice = c.Product.DiscountPrice ?? c.Product.Price,
                Quantity = c.Quantity
            }).ToList()
        };
    }

    public async Task<int> GetCartCountAsync(string userId) =>
        await _cartRepository.CountAsync(c => c.UserId == userId);

    public async Task ClearCartAsync(string userId)
    {
        var items = await _cartRepository.FindAsync(c => c.UserId == userId);
        _cartRepository.RemoveRange(items);
        await _cartRepository.SaveChangesAsync();
    }
}

public class OrderService : IOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly ICartService _cartService;

    public OrderService(IGenericRepository<Order> orderRepository, ICartService cartService)
    {
        _orderRepository = orderRepository;
        _cartService = cartService;
    }

    public async Task<Order> CreateOrderAsync(string userId, string shippingAddress, string? notes)
    {
        var cart = await _cartService.GetCartAsync(userId);
        if (!cart.Items.Any()) throw new InvalidOperationException("Cart is empty");

        using var transaction = await _orderRepository.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                TotalAmount = cart.Total,
                TaxAmount = cart.Tax,
                ShippingAddress = shippingAddress,
                Notes = notes,
                UserId = userId,
                Status = OrderStatus.Pending,
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
                OrderItems = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            // Clear cart
            await _cartService.ClearCartAsync(userId);

            await transaction.CommitAsync();
            return order;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId) =>
        await _orderRepository.Query()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetShopkeeperOrdersAsync(string shopkeeperId) =>
        await _orderRepository.Query()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Where(o => o.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<Order?> GetByIdAsync(int id, string? userId = null)
    {
        var query = _orderRepository.Query()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.Payment)
            .AsQueryable();

        if (userId != null)
        {
            query = query.Where(o => o.UserId == userId || o.OrderItems.Any(oi => oi.Product.ShopkeeperId == userId));
        }

        return await query.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync(int page = 1, int pageSize = 10) =>
        await _orderRepository.Query()
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetOrderCountAsync(OrderStatus? status = null, string? userId = null)
    {
        var query = _orderRepository.Query();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (userId != null) query = query.Where(o => o.UserId == userId);
        return await query.CountAsync();
    }

    public async Task<bool> UpdateShippingInfoAsync(int id, string trackingNumber, string carrier, OrderStatus newStatus)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return false;

        order.TrackingNumber = trackingNumber;
        order.Carrier = carrier;
        order.Status = newStatus;
        if (newStatus == OrderStatus.Shipped) order.ShippedDate = DateTime.UtcNow;
        if (newStatus == OrderStatus.Delivered) order.DeliveredDate = DateTime.UtcNow;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync();
        return true;
    }
}

public class PaymentService : IPaymentService
{
    private readonly IGenericRepository<ServicePlatform.Models.Payment> _repository;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public PaymentService(IGenericRepository<ServicePlatform.Models.Payment> repository, INotificationService notificationService, IConfiguration configuration)
    {
        _repository = repository;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    public async Task<ServicePlatform.Models.Payment> CreatePaymentAsync(string userId, decimal amount, PaymentGateway gateway, int? orderId = null, int? serviceRequestId = null)
    {
        var payment = new ServicePlatform.Models.Payment
        {
            Amount = amount,
            Gateway = gateway,
            UserId = userId,
            OrderId = orderId,
            ServiceRequestId = serviceRequestId,
            Status = PaymentStatus.Pending,
            TransactionId = Guid.NewGuid().ToString()
        };

        await _repository.AddAsync(payment);
        await _repository.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> UpdateGatewayOrderIdAsync(int paymentId, string gatewayOrderId)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null) return false;
        payment.GatewayOrderId = gatewayOrderId;
        _repository.Update(payment);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompletePaymentAsync(int paymentId, string transactionId, string? gatewayPaymentId = null, string? signature = null)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null) return false;

        payment.Status = PaymentStatus.Completed;
        payment.TransactionId = transactionId;
        payment.GatewayPaymentId = gatewayPaymentId;
        payment.GatewaySignature = signature;
        payment.CompletedAt = DateTime.UtcNow;

        _repository.Update(payment);
        await _repository.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(payment.UserId,
            "Payment Successful", $"Payment of ₹{payment.Amount} completed successfully.",
            NotificationType.PaymentCompleted);

        return true;
    }

    public async Task<string> InitializeRazorpayOrderAsync(int paymentId)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null) throw new ArgumentException("Payment not found");

        string keyId = _configuration["PaymentSettings:Razorpay:KeyId"] ?? "";
        string keySecret = _configuration["PaymentSettings:Razorpay:KeySecret"] ?? "";

        Razorpay.Api.RazorpayClient client = new Razorpay.Api.RazorpayClient(keyId, keySecret);
        Dictionary<string, object> options = new Dictionary<string, object>();
        options.Add("amount", (int)(payment.Amount * 100)); // amount in paise
        options.Add("currency", "INR");
        options.Add("receipt", $"receipt_{payment.Id}");

        Razorpay.Api.Order order = client.Order.Create(options);
        string razorpayOrderId = order["id"].ToString();

        payment.GatewayOrderId = razorpayOrderId;
        _repository.Update(payment);
        await _repository.SaveChangesAsync();

        return razorpayOrderId;
    }

    public async Task<string> InitializeStripeSessionAsync(int paymentId, string successUrl, string cancelUrl)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null) throw new ArgumentException("Payment not found");

        Stripe.StripeConfiguration.ApiKey = _configuration["PaymentSettings:Stripe:SecretKey"];

        var options = new Stripe.Checkout.SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
            {
                new Stripe.Checkout.SessionLineItemOptions
                {
                    PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(payment.Amount * 100),
                        Currency = "inr",
                        ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Service Platform Order #{payment.OrderId ?? payment.Id}",
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = payment.Id.ToString()
        };

        var service = new Stripe.Checkout.SessionService();
        Stripe.Checkout.Session session = await service.CreateAsync(options);

        payment.GatewayOrderId = session.Id;
        _repository.Update(payment);
        await _repository.SaveChangesAsync();

        return session.Id;
    }

    public async Task<bool> FailPaymentAsync(int paymentId, string reason)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null) return false;

        payment.Status = PaymentStatus.Failed;
        payment.FailureReason = reason;
        _repository.Update(payment);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ServicePlatform.Models.Payment>> GetUserPaymentsAsync(string userId) =>
        await _repository.Query()
            .Include(p => p.Order)
            .Include(p => p.ServiceRequest)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<ServicePlatform.Models.Payment>> GetAllPaymentsAsync(int page = 1, int pageSize = 10) =>
        await _repository.Query()
            .Include(p => p.User)
            .Include(p => p.Order)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<decimal> GetTotalRevenueAsync() =>
        await _repository.Query()
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

    public async Task<decimal> GetMonthlyRevenueAsync()
    {
        var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _repository.Query()
            .Where(p => p.Status == PaymentStatus.Completed && p.CompletedAt >= firstOfMonth)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
    }

    public async Task<ServicePlatform.Models.Payment?> GetByIdAsync(int id) =>
        await _repository.Query()
            .Include(p => p.User)
            .Include(p => p.Order)
            .Include(p => p.ServiceRequest)
            .FirstOrDefaultAsync(p => p.Id == id);
}
