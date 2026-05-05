using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Service;

namespace ServicePlatform.Controllers;

[Authorize(Roles = "User")]
public class UserDashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IServiceRequestService _requestService;
    private readonly IAppointmentService _appointmentService;
    private readonly IFeedbackService _feedbackService;
    private readonly IPaymentService _paymentService;
    private readonly IRatingService _ratingService;
    private readonly INotificationService _notificationService;
    private readonly IVehicleService _vehicleService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserDashboardController> _logger;

    public UserDashboardController(
        IDashboardService dashboardService,
        IServiceRequestService requestService,
        IAppointmentService appointmentService,
        IFeedbackService feedbackService,
        IPaymentService paymentService,
        IRatingService ratingService,
        INotificationService notificationService,
        IVehicleService vehicleService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _requestService = requestService;
        _appointmentService = appointmentService;
        _feedbackService = feedbackService;
        _paymentService = paymentService;
        _ratingService = ratingService;
        _notificationService = notificationService;
        _vehicleService = vehicleService;
        _userManager = userManager;
        _logger = logger;
    }

    private string UserId => _userManager.GetUserId(User)!;

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("User {UserId} accessed dashboard", UserId);
            var model = await _dashboardService.GetUserDashboardAsync(UserId);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user dashboard for {UserId}", UserId);
            TempData["Error"] = "Failed to load dashboard: " + ex.Message;
            return View();
        }
    }

    // ---- Service Requests ----
    [HttpGet]
    public async Task<IActionResult> CreateRequest()
    {
        ViewBag.VehicleModels = await _vehicleService.GetAllActiveModelsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(ServiceRequestViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            _logger.LogInformation("User {UserId} creating service request: {Title}", UserId, model.Title);
            await _requestService.CreateRequestAsync(model, UserId);
            TempData["Success"] = "Service request submitted successfully!";
            return RedirectToAction("MyRequests");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service request for {UserId}", UserId);
            TempData["Error"] = "Failed to submit service request.";
            return View(model);
        }
    }

    public async Task<IActionResult> MyRequests()
    {
        try
        {
            _logger.LogInformation("User {UserId} viewing service requests", UserId);
            var requests = await _requestService.GetUserRequestsAsync(UserId);
            return View(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service requests for {UserId}", UserId);
            TempData["Error"] = "Failed to load your requests: " + ex.Message;
            return View(new List<ServiceRequest>());
        }
    }

    public async Task<IActionResult> RequestDetails(int id)
    {
        try
        {
            var request = await _requestService.GetByIdAsync(id);
            if (request == null || request.UserId != UserId) return NotFound();
            _logger.LogInformation("User {UserId} viewing request details {RequestId}", UserId, id);
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading request details {RequestId} for {UserId}", id, UserId);
            TempData["Error"] = "Failed to load request details: " + ex.Message;
            return RedirectToAction("MyRequests");
        }
    }

    // ---- Appointments ----
    [HttpGet]
    public IActionResult BookAppointment() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(AppointmentViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            _logger.LogInformation("User {UserId} booking appointment: {Title}", UserId, model.Title);
            await _appointmentService.CreateAppointmentAsync(model, UserId);
            TempData["Success"] = "Appointment booked successfully!";
            return RedirectToAction("MyAppointments");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking appointment for {UserId}", UserId);
            TempData["Error"] = "Failed to book appointment.";
            return View(model);
        }
    }

    public async Task<IActionResult> MyAppointments()
    {
        try
        {
            _logger.LogInformation("User {UserId} viewing appointments", UserId);
            var appointments = await _appointmentService.GetUserAppointmentsAsync(UserId);
            return View(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading appointments for {UserId}", UserId);
            TempData["Error"] = "Failed to load appointments: " + ex.Message;
            return View(new List<Appointment>());
        }
    }

    // ---- Ratings ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRating(RatingViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction("RequestDetails", new { id = model.ServiceRequestId });

        try
        {
            _logger.LogInformation("User {UserId} submitting rating for request {RequestId}", UserId, model.ServiceRequestId);
            await _ratingService.CreateRatingAsync(model, UserId);
            TempData["Success"] = "Rating submitted successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting rating for {UserId}", UserId);
            TempData["Error"] = "Failed to submit rating.";
        }

        return RedirectToAction("RequestDetails", new { id = model.ServiceRequestId });
    }

    // ---- Feedback ----
    [HttpGet]
    public IActionResult Feedback() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(ViewModels.Admin.FeedbackViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            _logger.LogInformation("User {UserId} submitting feedback with rating {Rating}", UserId, model.Rating);
            await _feedbackService.CreateFeedbackAsync(UserId, model.Message, model.Rating);
            TempData["Success"] = "Feedback submitted successfully!";
            return RedirectToAction("MyFeedback");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback for {UserId}", UserId);
            TempData["Error"] = "Failed to submit feedback.";
            return View(model);
        }
    }

    public async Task<IActionResult> MyFeedback()
    {
        try
        {
            _logger.LogInformation("User {UserId} viewing feedback history", UserId);
            var feedbacks = await _feedbackService.GetUserFeedbacksAsync(UserId);
            return View(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feedbacks for {UserId}", UserId);
            TempData["Error"] = "Failed to load feedback history: " + ex.Message;
            return View(new List<Feedback>());
        }
    }

    // ---- Payments ----
    public async Task<IActionResult> PaymentHistory()
    {
        try
        {
            var userId = UserId;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            _logger.LogInformation("User {UserId} viewing payment history", userId);
            var payments = await _paymentService.GetUserPaymentsAsync(userId);
            
            return View(payments ?? new List<Payment>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment history for {UserId}", UserId);
            TempData["Error"] = "An internal error occurred while fetching your payment records.";
            return View(new List<Payment>());
        }
    }

    // ---- Policy ----
    public IActionResult Policy() => View();

    // ---- Settings ----
    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        try
        {
            var user = await _userManager.FindByIdAsync(UserId);
            return View(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings for {UserId}", UserId);
            TempData["Error"] = "Failed to load settings: " + ex.Message;
            return View();
        }
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

        try
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Mobile = model.Mobile;
            user.State = model.State;
            user.Address = model.Address;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);
            _logger.LogInformation("User {UserId} updated settings", UserId);
            TempData["Success"] = "Settings updated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for {UserId}", UserId);
            TempData["Error"] = "Failed to update settings.";
        }

        return RedirectToAction("Settings");
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment == null || payment.UserId != UserId) return NotFound();
        return View(payment);
    }

    public async Task<IActionResult> ServiceReminders()
    {
        var requests = await _requestService.GetUserRequestsAsync(UserId);
        var reminders = requests.Where(r => r.Status == ServiceRequestStatus.Completed && r.CompletedAt < DateTime.UtcNow.AddMonths(-6));
        return View(reminders);
    }

    // ---- Vehicle Profile ----
    public async Task<IActionResult> MyVehicles()
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var vehicles = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(db.UserVehicles.Where(v => v.UserId == UserId));
        return View(vehicles);
    }

    [HttpGet]
    public IActionResult AddVehicle() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVehicle(UserVehicle vehicle)
    {
        vehicle.UserId = UserId;
        if (!ModelState.IsValid) return View(vehicle);

        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        await db.UserVehicles.AddAsync(vehicle);
        await db.SaveChangesAsync();
        TempData["Success"] = "Vehicle added successfully!";
        return RedirectToAction("MyVehicles");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var vehicle = await db.UserVehicles.FindAsync(id);
        if (vehicle == null || vehicle.UserId != UserId) return NotFound();
        db.UserVehicles.Remove(vehicle);
        await db.SaveChangesAsync();
        TempData["Success"] = "Vehicle removed.";
        return RedirectToAction("MyVehicles");
    }

    // ---- Support Tickets ----
    [HttpGet]
    public IActionResult RaiseTicket() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseTicket(SupportTicket model)
    {
        model.UserId = UserId;
        if (!ModelState.IsValid) return View(model);

        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        await db.SupportTickets.AddAsync(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Support ticket submitted!";
        return RedirectToAction("MyTickets");
    }

    public async Task<IActionResult> MyTickets()
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var tickets = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(db.SupportTickets.Where(t => t.UserId == UserId).OrderByDescending(t => t.CreatedAt));
        return View(tickets);
    }

    // ---- Return Request ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReturn(int orderId, string reason)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var order = await db.Orders.FindAsync(orderId);
        if (order == null || order.UserId != UserId) return NotFound();
        if (order.Status != OrderStatus.Delivered)
        {
            TempData["Error"] = "Only delivered orders can be returned.";
            return RedirectToAction("PaymentHistory");
        }

        order.ReturnReason = reason;
        order.ReturnRequestedAt = DateTime.UtcNow;
        order.Status = OrderStatus.Returned;
        await db.SaveChangesAsync();
        TempData["Success"] = "Return request submitted!";
        return RedirectToAction("PaymentHistory");
    }
}
