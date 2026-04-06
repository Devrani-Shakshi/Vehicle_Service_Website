using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IServiceRequestService _requestService;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IFeedbackService _feedbackService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDashboardService dashboardService,
        IServiceRequestService requestService,
        IOrderService orderService,
        IPaymentService paymentService,
        IFeedbackService feedbackService,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _dashboardService = dashboardService;
        _requestService = requestService;
        _orderService = orderService;
        _paymentService = paymentService;
        _feedbackService = feedbackService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Admin dashboard accessed");
            var model = await _dashboardService.GetAdminDashboardAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            TempData["Error"] = "Failed to load dashboard.";
            return View();
        }
    }

    public async Task<IActionResult> Users(string? role = null)
    {
        try
        {
            var users = role switch
            {
                "User" => await _userManager.GetUsersInRoleAsync("User"),
                "ServiceProvider" => await _userManager.GetUsersInRoleAsync("ServiceProvider"),
                "Shopkeeper" => await _userManager.GetUsersInRoleAsync("Shopkeeper"),
                _ => _userManager.Users.ToList()
            };
            ViewBag.SelectedRole = role;
            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users list");
            TempData["Error"] = "Failed to load users.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully!";
        return RedirectToAction("Users");
    }

    public async Task<IActionResult> ServiceRequests()
    {
        try
        {
            _logger.LogInformation("Admin viewing all service requests");
            var requests = await _requestService.GetAllRequestsAsync();
            return View(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service requests");
            TempData["Error"] = "Failed to load service requests.";
            return View(new List<ServicePlatform.Models.ServiceRequest>());
        }
    }

    public async Task<IActionResult> ServiceRequestDetails(int id)
    {
        var request = await _requestService.GetByIdAsync(id);
        if (request == null) return NotFound();
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequestStatus(int id, ServicePlatform.Models.ServiceRequestStatus status)
    {
        await _requestService.UpdateStatusAsync(id, status);
        TempData["Success"] = "Request status updated!";
        return RedirectToAction("ServiceRequestDetails", new { id });
    }

    public async Task<IActionResult> Orders()
    {
        try
        {
            _logger.LogInformation("Admin viewing all orders");
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading orders");
            TempData["Error"] = "Failed to load orders.";
            return View(new List<ServicePlatform.Models.Order>());
        }
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, ServicePlatform.Models.OrderStatus status)
    {
        await _orderService.UpdateStatusAsync(id, status);
        TempData["Success"] = "Order status updated!";
        return RedirectToAction("OrderDetails", new { id });
    }

    public async Task<IActionResult> Payments()
    {
        try
        {
            _logger.LogInformation("Admin viewing all payments");
            var payments = await _paymentService.GetAllPaymentsAsync();
            return View(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payments");
            TempData["Error"] = "Failed to load payments.";
            return View(new List<ServicePlatform.Models.Payment>());
        }
    }

    public async Task<IActionResult> Feedbacks()
    {
        try
        {
            _logger.LogInformation("Admin viewing all feedbacks");
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync();
            return View(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feedbacks");
            TempData["Error"] = "Failed to load feedbacks.";
            return View(new List<ServicePlatform.Models.Feedback>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveFeedback(int id, string adminResponse)
    {
        try
        {
            _logger.LogInformation("Admin resolving feedback {FeedbackId}", id);
            await _feedbackService.ResolveFeedbackAsync(id, adminResponse);
            TempData["Success"] = "Feedback resolved!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving feedback {FeedbackId}", id);
            TempData["Error"] = "Failed to resolve feedback.";
        }

        return RedirectToAction("Feedbacks");
    }

    // API endpoint for chart data
    [HttpGet]
    public async Task<IActionResult> GetDashboardData()
    {
        try
        {
            var model = await _dashboardService.GetAdminDashboardAsync();
            return Json(new
            {
                revenue = model.RevenueData,
                activity = model.ServiceActivityData,
                growth = model.UserGrowthData,
                stats = new
                {
                    model.TotalUsers,
                    model.TotalServiceProviders,
                    model.TotalShopkeepers,
                    model.TotalOrders,
                    model.TotalServiceRequests,
                    model.TotalRevenue,
                    model.MonthlyRevenue
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard data API");
            return Json(new { error = "Failed to load dashboard data" });
        }
    }
}
