using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

    public ShopkeeperController(
        IDashboardService dashboardService,
        IProductService productService,
        IOrderService orderService,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager,
        ILogger<ShopkeeperController> logger)
    {
        _dashboardService = dashboardService;
        _productService = productService;
        _orderService = orderService;
        _paymentService = paymentService;
        _userManager = userManager;
        _logger = logger;
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
            TempData["Error"] = "Failed to load dashboard.";
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
            TempData["Error"] = "Failed to load products.";
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
            TempData["Error"] = "Failed to add product.";
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
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Category = product.Category,
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
            TempData["Error"] = "Failed to load product for editing.";
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
            TempData["Error"] = "Failed to update product.";
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
            TempData["Error"] = "Failed to load orders.";
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
            TempData["Error"] = "Failed to load order details.";
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
}
