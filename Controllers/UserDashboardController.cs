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
            TempData["Error"] = "Failed to load dashboard.";
            return View();
        }
    }

    // ---- Service Requests ----
    [HttpGet]
    public IActionResult CreateRequest() => View();

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
            TempData["Error"] = "Failed to load your requests.";
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
            TempData["Error"] = "Failed to load request details.";
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
            TempData["Error"] = "Failed to load appointments.";
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
    public async Task<IActionResult> Feedback(string subject, string message, string? category)
    {
        try
        {
            _logger.LogInformation("User {UserId} submitting feedback: {Subject}", UserId, subject);
            await _feedbackService.CreateFeedbackAsync(UserId, subject, message, category);
            TempData["Success"] = "Feedback submitted successfully!";
            return RedirectToAction("MyFeedback");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback for {UserId}", UserId);
            TempData["Error"] = "Failed to submit feedback.";
            return View();
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
            TempData["Error"] = "Failed to load feedback history.";
            return View(new List<Feedback>());
        }
    }

    // ---- Payments ----
    public async Task<IActionResult> PaymentHistory()
    {
        try
        {
            _logger.LogInformation("User {UserId} viewing payment history", UserId);
            var payments = await _paymentService.GetUserPaymentsAsync(UserId);
            return View(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payment history for {UserId}", UserId);
            TempData["Error"] = "Failed to load payment history.";
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
            TempData["Error"] = "Failed to load settings.";
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(string fullName, string mobile, string state, string? address)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.Mobile = mobile;
            user.State = state;
            user.Address = address;
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
}
