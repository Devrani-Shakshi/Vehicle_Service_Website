using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Shop;

namespace ServicePlatform.Controllers;

[Authorize]
public class ShopController : Controller
{
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ShopController> _logger;
    private readonly IConfiguration _configuration;

    public ShopController(
        IProductService productService,
        ICartService cartService,
        IOrderService orderService,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager,
        ILogger<ShopController> logger,
        IConfiguration configuration)
    {
        _productService = productService;
        _cartService = cartService;
        _orderService = orderService;
        _paymentService = paymentService;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
    }

    private string UserId => _userManager.GetUserId(User)!;

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? category, string? sortBy, int page = 1)
    {
        try
        {
            _logger.LogInformation("Shop catalog accessed — Search: {Search}, Category: {Category}, Page: {Page}", search, category, page);
            var catalog = await _productService.GetCatalogAsync(search, category, sortBy, page, 12);
            return View(catalog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shop catalog");
            TempData["Error"] = "Failed to load product catalog.";
            return View();
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> ProductDetails(int id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            _logger.LogInformation("Product details viewed: {ProductId} - {ProductName}", id, product.Name);
            return View(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product details for {ProductId}", id);
            TempData["Error"] = "Failed to load product details.";
            return RedirectToAction("Index");
        }
    }

    // ---- Cart ----
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        try
        {
            _logger.LogInformation("User {UserId} adding product {ProductId} to cart (qty: {Qty})", UserId, productId, quantity);
            var result = await _cartService.AddToCartAsync(UserId, productId, quantity);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = result, cartCount = await _cartService.GetCartCountAsync(UserId) });
            TempData["Success"] = "Product added to cart!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product {ProductId} to cart for {UserId}", productId, UserId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, error = "Failed to add to cart" });
            TempData["Error"] = "Failed to add product to cart.";
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Cart()
    {
        try
        {
            _logger.LogInformation("User {UserId} viewing cart", UserId);
            var cart = await _cartService.GetCartAsync(UserId);
            return View(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cart for {UserId}", UserId);
            TempData["Error"] = "Failed to load cart.";
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCartItem(int cartItemId, int quantity)
    {
        try
        {
            _logger.LogInformation("User {UserId} updating cart item {CartItemId} to qty {Qty}", UserId, cartItemId, quantity);
            await _cartService.UpdateQuantityAsync(cartItemId, quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item {CartItemId}", cartItemId);
            TempData["Error"] = "Failed to update cart item.";
        }

        return RedirectToAction("Cart");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveCartItem(int cartItemId)
    {
        try
        {
            _logger.LogInformation("User {UserId} removing cart item {CartItemId}", UserId, cartItemId);
            await _cartService.RemoveFromCartAsync(cartItemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
            TempData["Error"] = "Failed to remove cart item.";
        }

        return RedirectToAction("Cart");
    }

    // ---- Checkout ----
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        try
        {
            _logger.LogInformation("User {UserId} accessing checkout", UserId);
            var cart = await _cartService.GetCartAsync(UserId);
            if (!cart.Items.Any()) return RedirectToAction("Cart");
            return View(new CheckoutViewModel { Cart = cart });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checkout for {UserId}", UserId);
            TempData["Error"] = "Failed to load checkout.";
            return RedirectToAction("Cart");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        try
        {
            _logger.LogInformation("User {UserId} placing order", UserId);
            var order = await _orderService.CreateOrderAsync(UserId, model.ShippingAddress, model.Notes);
            var gateway = model.PaymentGateway == "Razorpay" ? PaymentGateway.Razorpay : PaymentGateway.Stripe;
            var payment = await _paymentService.CreatePaymentAsync(UserId, order.TotalAmount, gateway, orderId: order.Id);
            _logger.LogInformation("Order {OrderId} created with payment {PaymentId} for {UserId}", order.Id, payment.Id, UserId);
            return RedirectToAction("ProcessPayment", new { paymentId = payment.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order for {UserId}", UserId);
            TempData["Error"] = "Failed to place order. Please try again.";
            return RedirectToAction("Checkout");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ProcessPayment(int paymentId)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(paymentId);
            if (payment == null || payment.UserId != UserId) return NotFound();

            if (payment.Gateway == PaymentGateway.Razorpay)
            {
                string razorpayOrderId = await _paymentService.InitializeRazorpayOrderAsync(paymentId);
                ViewBag.RazorpayKeyId = _configuration["PaymentSettings:Razorpay:KeyId"];
            }
            else if (payment.Gateway == PaymentGateway.Stripe)
            {
                var successUrl = Url.Action("StripeSuccess", "Shop", new { paymentId = payment.Id }, Request.Scheme) ?? "";
                var cancelUrl = Url.Action("ProcessPayment", "Shop", new { paymentId = payment.Id }, Request.Scheme) ?? "";
                string sessionId = await _paymentService.InitializeStripeSessionAsync(paymentId, successUrl, cancelUrl);
                ViewBag.StripeSessionId = sessionId;
                ViewBag.StripePublishableKey = _configuration["PaymentSettings:Stripe:PublishableKey"];
            }

            _logger.LogInformation("User {UserId} processing payment {PaymentId}", UserId, paymentId);
            return View(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment {PaymentId} for {UserId}", paymentId, UserId);
            TempData["Error"] = "Failed to initialize payment gateway.";
            return RedirectToAction("MyOrders");
        }
    }

    public async Task<IActionResult> StripeSuccess(int paymentId, string session_id)
    {
        try
        {
            _logger.LogInformation("Stripe success for payment {PaymentId}, session {SessionId}", paymentId, session_id);
            await _paymentService.CompletePaymentAsync(paymentId, session_id);
            TempData["Success"] = "Payment completed successfully!";
            return RedirectToAction("OrderConfirmation", new { paymentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing Stripe payment {PaymentId}", paymentId);
            TempData["Error"] = "Payment processing failed.";
            return RedirectToAction("MyOrders");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompletePayment(int paymentId, string transactionId)
    {
        try
        {
            _logger.LogInformation("User {UserId} completing payment {PaymentId} with txn {TxnId}", UserId, paymentId, transactionId);
            await _paymentService.CompletePaymentAsync(paymentId, transactionId);
            TempData["Success"] = "Payment completed successfully!";
            return RedirectToAction("OrderConfirmation", new { paymentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing payment {PaymentId}", paymentId);
            TempData["Error"] = "Payment processing failed. Please try again.";
            return RedirectToAction("ProcessPayment", new { paymentId });
        }
    }

    public async Task<IActionResult> OrderConfirmation(int paymentId)
    {
        try
        {
            var payment = await _paymentService.GetByIdAsync(paymentId);
            if (payment == null || payment.UserId != UserId) return NotFound();
            _logger.LogInformation("User {UserId} viewing order confirmation for payment {PaymentId}", UserId, paymentId);
            return View(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading order confirmation for payment {PaymentId}", paymentId);
            TempData["Error"] = "Failed to load confirmation.";
            return RedirectToAction("MyOrders");
        }
    }

    public async Task<IActionResult> MyOrders()
    {
        try
        {
            _logger.LogInformation("User {UserId} viewing orders", UserId);
            var orders = await _orderService.GetUserOrdersAsync(UserId);
            return View(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders for {UserId}", UserId);
            TempData["Error"] = "Failed to load orders.";
            return View(new List<ServicePlatform.Models.Order>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        try
        {
            var count = await _cartService.GetCartCountAsync(UserId);
            return Json(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart count for {UserId}", UserId);
            return Json(new { count = 0 });
        }
    }
}
