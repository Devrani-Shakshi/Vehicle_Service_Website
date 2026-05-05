using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicePlatform.Data;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Shop;

namespace ServicePlatform.Controllers;

[Authorize(Roles = "Shopkeeper")]
public class ShopkeeperController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ShopkeeperController> _logger;
    private readonly ApplicationDbContext _context;

    public ShopkeeperController(
        IDashboardService dashboardService,
        IProductService productService,
        IOrderService orderService,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager,
        ILogger<ShopkeeperController> logger,
        ApplicationDbContext context)
    {
        _dashboardService = dashboardService;
        _productService = productService;
        _orderService = orderService;
        _paymentService = paymentService;
        _userManager = userManager;
        _logger = logger;
        _context = context;
    }

    private string ShopkeeperId => _userManager.GetUserId(User)!;

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} accessed dashboard", ShopkeeperId);
            var model = await _dashboardService.GetShopkeeperDashboardAsync(ShopkeeperId);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shopkeeper dashboard for {ShopkeeperId}", ShopkeeperId);
            TempData["Error"] = "Failed to load dashboard: " + ex.Message;
            return View();
        }
    }

    // ---- Product Management ----
    public async Task<IActionResult> Products()
    {
        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} viewing products", ShopkeeperId);
            var products = await _productService.GetShopkeeperProductsAsync(ShopkeeperId);
            return View(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products for shopkeeper {ShopkeeperId}", ShopkeeperId);
            TempData["Error"] = "Failed to load products: " + ex.Message;
            return View(new List<Product>());
        }
    }

    [HttpGet]
    public IActionResult AddProduct() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(ProductViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} adding product: {Name}", ShopkeeperId, model.Name);
            await _productService.CreateProductAsync(model, ShopkeeperId);
            TempData["Success"] = "Product added successfully!";
            return RedirectToAction("Products");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product for shopkeeper {ShopkeeperId}", ShopkeeperId);
            TempData["Error"] = "Failed to add product: " + ex.Message;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null || product.ShopkeeperId != ShopkeeperId) return NotFound();

            _logger.LogInformation("Shopkeeper {ShopkeeperId} editing product {ProductId}", ShopkeeperId, id);
            var model = new ProductViewModel
            {
                Name = product.Name,
                Description = product.Description ?? string.Empty,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Category = product.Category ?? string.Empty,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity,
                SKU = product.SKU
            };
            ViewBag.ProductId = id;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product {ProductId} for edit", id);
            TempData["Error"] = "Failed to load product for editing: " + ex.Message;
            return RedirectToAction("Products");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, ProductViewModel model)
    {
        if (!ModelState.IsValid) { ViewBag.ProductId = id; return View(model); }

        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} updating product {ProductId}", ShopkeeperId, id);
            await _productService.UpdateProductAsync(id, model, ShopkeeperId);
            TempData["Success"] = "Product updated successfully!";
            return RedirectToAction("Products");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            TempData["Error"] = "Failed to update product: " + ex.Message;
            ViewBag.ProductId = id;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} deleting product {ProductId}", ShopkeeperId, id);
            await _productService.DeleteProductAsync(id, ShopkeeperId);
            TempData["Success"] = "Product deleted successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            TempData["Error"] = "Failed to delete product.";
        }

        return RedirectToAction("Products");
    }

    // ---- Orders ----
    public async Task<IActionResult> Orders()
    {
        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} viewing orders", ShopkeeperId);
            var orders = await _orderService.GetShopkeeperOrdersAsync(ShopkeeperId);
            return View(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders for shopkeeper {ShopkeeperId}", ShopkeeperId);
            TempData["Error"] = "Failed to load orders: " + ex.Message;
            return View(new List<Order>());
        }
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null) return NotFound();
            _logger.LogInformation("Shopkeeper {ShopkeeperId} viewing order {OrderId}", ShopkeeperId, id);
            return View(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading order details {OrderId}", id);
            TempData["Error"] = "Failed to load order details: " + ex.Message;
            return RedirectToAction("Orders");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
    {
        try
        {
            _logger.LogInformation("Shopkeeper {ShopkeeperId} updating order {OrderId} to {Status}", ShopkeeperId, id, status);
            await _orderService.UpdateStatusAsync(id, status);
            TempData["Success"] = "Order status updated!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} status", id);
            TempData["Error"] = "Failed to update order status.";
        }

        return RedirectToAction("OrderDetails", new { id });
    }

    // ---- Shipping & Tracking ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingInfo(ShipmentUpdateViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction("OrderDetails", new { id = model.OrderId });

        try
        {
            var success = await _orderService.UpdateShippingInfoAsync(model.OrderId, model.TrackingNumber, model.Carrier, model.NewStatus);
            if (!success) return NotFound();

            TempData["Success"] = "Shipping information updated!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping info for order {OrderId}", model.OrderId);
            TempData["Error"] = "Failed to update shipping information.";
        }

        return RedirectToAction("OrderDetails", new { id = model.OrderId });
    }

    // ---- Inventory & Stock ----
    public async Task<IActionResult> Inventory()
    {
        var products = await _productService.GetShopkeeperProductsAsync(ShopkeeperId);
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int productId, int quantity)
    {
        try
        {
            var success = await _productService.UpdateStockAsync(productId, quantity, ShopkeeperId);
            if (!success) return NotFound();

            TempData["Success"] = "Stock updated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
            TempData["Error"] = "Failed to update stock.";
        }

        return RedirectToAction("Inventory");
    }

    // ---- Payments ----
    public async Task<IActionResult> Payments()
    {
        var payments = await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Order)
            .Where(p => p.Order != null && p.Order.OrderItems.Any(oi => oi.Product.ShopkeeperId == ShopkeeperId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(payments);
    }

    // ---- Reports ----
    public async Task<IActionResult> DownloadReport(string type)
    {
        var csv = "";
        var fileName = $"Shop_{type}_Report_{DateTime.Now:yyyyMMdd}.csv";

        if (type == "orders")
        {
            var orders = await _orderService.GetShopkeeperOrdersAsync(ShopkeeperId);
            var header = Helpers.CsvHelper.BuildCsvRow("Order #", "Customer", "Amount", "Status", "Date");
            var rows = orders.Select(o => Helpers.CsvHelper.BuildCsvRow(o.OrderNumber, o.User?.FullName, o.TotalAmount.ToString(), o.Status.ToString(), o.CreatedAt.ToString()));
            csv = header + "\n" + string.Join("\n", rows);
        }
        else if (type == "inventory")
        {
            var products = await _productService.GetShopkeeperProductsAsync(ShopkeeperId);
            var header = Helpers.CsvHelper.BuildCsvRow("SKU", "Product", "Category", "Price", "Stock");
            var rows = products.Select(p => Helpers.CsvHelper.BuildCsvRow(p.SKU, p.Name, p.Category, p.Price.ToString(), p.StockQuantity.ToString()));
            csv = header + "\n" + string.Join("\n", rows);
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    // ---- Settings & Policy ----
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(ViewModels.Account.ProfileSettingsViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // State Whitelist Validation
        if (!Helpers.AppConstants.AllStates.Contains(model.State))
        {
            ModelState.AddModelError("State", "Invalid state selected.");
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Mobile = model.Mobile;
        user.State = model.State;
        user.Address = model.Address;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Profile settings updated!";
        return RedirectToAction("Settings");
    }

    public IActionResult Policy() => View();

    // ---- Inventory & Reviews ----
    public async Task<IActionResult> LowStock()
    {
        var lowStockProducts = await _context.Products
            .Where(p => p.ShopkeeperId == ShopkeeperId && p.StockQuantity < 5 && !p.IsDeleted)
            .ToListAsync();
        return View(lowStockProducts);
    }

    public async Task<IActionResult> ProductReviews()
    {
        var reviews = await _context.Ratings
            .Include(r => r.User)
            .ToListAsync();
        return View(reviews);
    }

    // ---- Return Management ----
    public async Task<IActionResult> Returns()
    {
        var returnedOrders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Where(o => o.Status == OrderStatus.Returned &&
                   o.OrderItems.Any(oi => oi.Product.ShopkeeperId == ShopkeeperId))
            .OrderByDescending(o => o.ReturnRequestedAt)
            .ToListAsync();
        return View(returnedOrders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessReturn(int orderId, bool approve)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.IsReturnApproved = approve;
        if (!approve) order.Status = OrderStatus.Delivered; // Reject return
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = approve ? "Return approved." : "Return rejected.";
        return RedirectToAction("Returns");
    }
}
