using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Controllers;

[Authorize(Roles = "ServiceProvider")]
public class ServiceProviderController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IServiceRequestService _requestService;
    private readonly IRatingService _ratingService;
    private readonly IAppointmentService _appointmentService;
    private readonly IPaymentService _paymentService;
    private readonly IVehicleService _vehicleService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ServiceProviderController> _logger;

    public ServiceProviderController(
        IDashboardService dashboardService,
        IServiceRequestService requestService,
        IRatingService ratingService,
        IAppointmentService appointmentService,
        IPaymentService paymentService,
        IVehicleService vehicleService,
        UserManager<ApplicationUser> userManager,
        ILogger<ServiceProviderController> logger)
    {
        _dashboardService = dashboardService;
        _requestService = requestService;
        _ratingService = ratingService;
        _appointmentService = appointmentService;
        _paymentService = paymentService;
        _vehicleService = vehicleService;
        _userManager = userManager;
        _logger = logger;
    }

    private string ProviderId => _userManager.GetUserId(User)!;

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} accessed dashboard", ProviderId);
            var model = await _dashboardService.GetProviderDashboardAsync(ProviderId);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading provider dashboard for {ProviderId}", ProviderId);
            TempData["Error"] = "Failed to load dashboard: " + ex.Message;
            return View();
        }
    }

    public async Task<IActionResult> PendingRequests()
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} viewing pending requests", ProviderId);
            var requests = await _requestService.GetProviderRequestsAsync(status: ServiceRequestStatus.Pending);
            return View(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending requests for {ProviderId}", ProviderId);
            TempData["Error"] = "Failed to load pending requests: " + ex.Message;
            return View(new List<ServiceRequest>());
        }
    }

    public async Task<IActionResult> MyRequests()
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} viewing assigned requests", ProviderId);
            var requests = await _requestService.GetProviderRequestsAsync(ProviderId);
            return View(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assigned requests for {ProviderId}", ProviderId);
            TempData["Error"] = "Failed to load your requests.";
            return View(new List<ServiceRequest>());
        }
    }

    public async Task<IActionResult> RequestDetails(int id)
    {
        try
        {
            var request = await _requestService.GetByIdAsync(id);
            if (request == null) return NotFound();
            _logger.LogInformation("ServiceProvider {ProviderId} viewing request {RequestId}", ProviderId, id);
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading request details {RequestId}", id);
            TempData["Error"] = "Failed to load request details.";
            return RedirectToAction("MyRequests");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRequest(int id)
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} accepting request {RequestId}", ProviderId, id);
            await _requestService.AcceptRequestAsync(id, ProviderId);
            TempData["Success"] = "Request accepted successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting request {RequestId} by {ProviderId}", id, ProviderId);
            TempData["Error"] = "Failed to accept request.";
        }

        return RedirectToAction("MyRequests");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id)
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} rejecting request {RequestId}", ProviderId, id);
            await _requestService.RejectRequestAsync(id, ProviderId);
            TempData["Success"] = "Request rejected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {RequestId} by {ProviderId}", id, ProviderId);
            TempData["Error"] = "Failed to reject request.";
        }

        return RedirectToAction("PendingRequests");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteRequest(int id)
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} completing request {RequestId}", ProviderId, id);
            await _requestService.CompleteRequestAsync(id);
            TempData["Success"] = "Request marked as completed!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing request {RequestId}", id);
            TempData["Error"] = "Failed to complete request.";
        }

        return RedirectToAction("MyRequests");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ServiceRequestStatus status)
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} updating request {RequestId} to {Status}", ProviderId, id, status);
            await _requestService.UpdateStatusAsync(id, status);
            TempData["Success"] = "Status updated!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for request {RequestId}", id);
            TempData["Error"] = "Failed to update status.";
        }

        return RedirectToAction("RequestDetails", new { id });
    }

    public async Task<IActionResult> CompletedRequests()
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} viewing completed requests", ProviderId);
            var requests = await _requestService.GetProviderRequestsAsync(ProviderId, ServiceRequestStatus.Completed);
            return View(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading completed requests for {ProviderId}", ProviderId);
            TempData["Error"] = "Failed to load completed requests: " + ex.Message;
            return View(new List<ServiceRequest>());
        }
    }

    public async Task<IActionResult> Ratings()
    {
        try
        {
            _logger.LogInformation("ServiceProvider {ProviderId} viewing ratings", ProviderId);
            var ratings = await _ratingService.GetProviderRatingsAsync(ProviderId);
            ViewBag.AverageRating = await _ratingService.GetProviderAverageRatingAsync(ProviderId);
            ViewBag.TotalReviews = await _ratingService.GetProviderReviewCountAsync(ProviderId);
            return View(ratings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading ratings for {ProviderId}", ProviderId);
            TempData["Error"] = "Failed to load ratings: " + ex.Message;
            return View(new List<Rating>());
        }
    }

    // Map view for service request location
    public async Task<IActionResult> ViewLocation(int id)
    {
        try
        {
            var request = await _requestService.GetByIdAsync(id);
            if (request == null) return NotFound();
            _logger.LogInformation("ServiceProvider {ProviderId} viewing location for request {RequestId}", ProviderId, id);
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading location for request {RequestId}", id);
            TempData["Error"] = "Failed to load location.";
            return RedirectToAction("MyRequests");
        }
    }

    // Settings & Policy
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

    // ── Vehicle Model Management (OEM Fleet) ─────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> VehicleModels()
    {
        var models = await _vehicleService.GetProviderModelsAsync(ProviderId);
        return View(models);
    }

    [HttpGet]
    public IActionResult CreateVehicleModel() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVehicleModel(VehicleModel model)
    {
        if (!ModelState.IsValid) return View(model);

        model.ServiceProviderId = ProviderId;
        await _vehicleService.CreateModelAsync(model);
        
        TempData["Success"] = "Vehicle model added to your fleet!";
        return RedirectToAction("VehicleModels");
    }

    [HttpGet]
    public async Task<IActionResult> EditVehicleModel(int id)
    {
        var model = await _vehicleService.GetByIdAsync(id);
        if (model == null || model.ServiceProviderId != ProviderId) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditVehicleModel(int id, VehicleModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _vehicleService.UpdateModelAsync(id, model, ProviderId);
        if (!result) return NotFound();

        TempData["Success"] = "Vehicle model updated successfully!";
        return RedirectToAction("VehicleModels");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVehicleModel(int id)
    {
        var result = await _vehicleService.DeleteModelAsync(id, ProviderId);
        if (!result) return NotFound();

        TempData["Success"] = "Vehicle model removed from your fleet.";
        return RedirectToAction("VehicleModels");
    }

    public async Task<IActionResult> Earnings()
    {
        var requests = await _requestService.GetProviderRequestsAsync(ProviderId, ServiceRequestStatus.Completed);
        return View(requests);
    }

    // ---- Sub-Technician Management ----
    public async Task<IActionResult> Technicians()
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var techs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(db.SubTechnicians.Where(t => t.ProviderId == ProviderId));
        return View(techs);
    }

    [HttpGet]
    public IActionResult AddTechnician() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTechnician(SubTechnician model)
    {
        model.ProviderId = ProviderId;
        if (!ModelState.IsValid) return View(model);

        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        await db.SubTechnicians.AddAsync(model);
        await db.SaveChangesAsync();
        TempData["Success"] = "Technician added!";
        return RedirectToAction("Technicians");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTechAvailability(int id)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var tech = await db.SubTechnicians.FindAsync(id);
        if (tech == null || tech.ProviderId != ProviderId) return NotFound();
        tech.IsAvailable = !tech.IsAvailable;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Technician {(tech.IsAvailable ? "enabled" : "disabled")}!";
        return RedirectToAction("Technicians");
    }

    // ---- Profile Verification ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCertifications(string certifications, string specializations)
    {
        var user = await _userManager.FindByIdAsync(ProviderId);
        if (user == null) return NotFound();
        user.Certifications = certifications;
        user.Specializations = specializations;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Certifications updated! Pending Admin verification.";
        return RedirectToAction("Settings");
    }
}
