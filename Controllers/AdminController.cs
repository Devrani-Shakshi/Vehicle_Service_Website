using Microsoft.EntityFrameworkCore;
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
    private readonly IConfiguration _configuration;
    private readonly Repositories.Interfaces.IGenericRepository<ActivityLog> _activityLogRepo;
    private readonly Repositories.Interfaces.IGenericRepository<GlobalSetting> _settingsRepo;

    public AdminController(
        IDashboardService dashboardService,
        IServiceRequestService requestService,
        IOrderService orderService,
        IPaymentService paymentService,
        IFeedbackService feedbackService,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger,
        IConfiguration configuration,
        Repositories.Interfaces.IGenericRepository<ActivityLog> activityLogRepo,
        Repositories.Interfaces.IGenericRepository<GlobalSetting> settingsRepo)
    {
        _dashboardService = dashboardService;
        _requestService = requestService;
        _orderService = orderService;
        _paymentService = paymentService;
        _feedbackService = feedbackService;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
        _activityLogRepo = activityLogRepo;
        _settingsRepo = settingsRepo;
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
            TempData["Error"] = "Failed to load dashboard: " + ex.Message;
            return View();
        }
    }

    public async Task<IActionResult> Users(string? role = null, int page = 1, int pageSize = 10)
    {
        try
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                // In Identity, to filter by role efficiently from the queryable
                // we usually join with UserRoles, but simpler here for the project:
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var totalUsers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query);
            var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                query.OrderBy(u => u.UserName)
                     .Skip((page - 1) * pageSize)
                     .Take(pageSize)
            );

            ViewBag.SelectedRole = role;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading users list");
            TempData["Error"] = "Failed to load users: " + ex.Message;
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

    public async Task<IActionResult> Orders(int page = 1, int pageSize = 10)
    {
        var orders = await _orderService.GetAllOrdersAsync(page, pageSize);
        var totalOrders = await _orderService.GetOrderCountAsync();
        
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
        
        return View(orders);
    }

    public async Task<IActionResult> ServiceRequests(int page = 1, int pageSize = 10)
    {
        var requests = await _requestService.GetAllRequestsAsync(page, pageSize);
        var totalRequests = await _requestService.GetTotalCountAsync();
        
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRequests / (double)pageSize); // This needs better count
        
        return View(requests);
    }

    [HttpGet]
    public IActionResult CreateUser() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(ViewModels.Admin.UserCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // State Whitelist Validation
        if (!Helpers.AppConstants.AllStates.Contains(model.State!))
        {
            ModelState.AddModelError("State", "Invalid state selected.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Mobile = model.Mobile,
            State = model.State,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = "User created successfully!";
            return RedirectToAction("Users");
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return View(new ViewModels.Admin.UserEditViewModel
        {
            Id = id,
            Email = user.Email!,
            FullName = user.FullName,
            Mobile = user.Mobile,
            State = user.State,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(ViewModels.Admin.UserEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // State Whitelist Validation
        if (!string.IsNullOrEmpty(model.State) && !Helpers.AppConstants.AllStates.Contains(model.State))
        {
            ModelState.AddModelError("State", "Invalid state selected.");
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Mobile = model.Mobile;
        user.State = model.State;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "User updated successfully!";
            return RedirectToAction("Users");
        }

        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsDeleted = true; // Soft delete
        await _userManager.UpdateAsync(user);
        TempData["Success"] = "User deleted successfully!";
        return RedirectToAction("Users");
    }

    // ---- Commissions ----
    public async Task<IActionResult> Commissions()
    {
        var commissions = await _dashboardService.GetCommissionsAsync();
        return View(commissions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCommissionPaid(int id)
    {
        await _dashboardService.MarkCommissionPaidAsync(id);
        TempData["Success"] = "Commission marked as paid!";
        return RedirectToAction("Commissions");
    }

    // ---- Reports ----
    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var model = await _dashboardService.GetAdminDashboardAsync();
        return View(model);
    }

    public async Task<IActionResult> DownloadReport(string type)
    {
        var csv = "";
        var fileName = $"{type}_Report_{DateTime.Now:yyyyMMdd}.csv";

        switch (type.ToLower())
        {
            case "users":
                var users = _userManager.Users.ToList();
                var header = Helpers.CsvHelper.BuildCsvRow("ID", "Full Name", "Email", "Mobile", "Role", "Status");
                var rows = users.Select(u => Helpers.CsvHelper.BuildCsvRow(u.Id, u.FullName, u.Email, u.Mobile, "User", u.IsActive ? "Active" : "Inactive"));
                csv = header + "\n" + string.Join("\n", rows);
                break;
            case "payments":
                var payments = await _paymentService.GetAllPaymentsAsync();
                var pHeader = Helpers.CsvHelper.BuildCsvRow("ID", "User", "Amount", "Gateway", "Status", "Date");
                var pRows = payments.Select(p => Helpers.CsvHelper.BuildCsvRow(p.Id.ToString(), p.User?.FullName, p.Amount.ToString(), p.Gateway.ToString(), p.Status.ToString(), p.CreatedAt.ToString()));
                csv = pHeader + "\n" + string.Join("\n", pRows);
                break;
            case "commissions":
                var commRate = _configuration.GetValue<int>("GlobalSettings:CommissionRate", 5);
                var commissions = await _dashboardService.GetCommissionsAsync();
                var cHeader = Helpers.CsvHelper.BuildCsvRow("ID", "Vendor Name", "Role", "Month", "Year", "Total Income", $"Commission ({commRate}%)", "Status");
                var cRows = commissions.Select(c => Helpers.CsvHelper.BuildCsvRow(
                    c.Id.ToString(), 
                    c.User?.FullName, 
                    c.User != null ? string.Join("|", _userManager.GetRolesAsync(c.User).Result) : "",
                    c.Month.ToString(),
                    c.Year.ToString(),
                    c.TotalIncome.ToString(),
                    c.CommissionAmount.ToString(),
                    c.IsPaid ? "Paid" : "Pending"
                ));
                csv = cHeader + "\n" + string.Join("\n", cRows);
                break;
        }

        // Add UTF-8 BOM for Excel compatibility
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var fileBytes = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, fileBytes, bom.Length, bytes.Length);

        return File(fileBytes, "text/csv", fileName);
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

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Settings updated successfully!";
        }
        else
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        }

        return RedirectToAction("Settings");
    }

    public IActionResult Policy() => View();



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
            TempData["Error"] = "Failed to load feedbacks: " + ex.Message;
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
                growth = model.UserGrowthData,
                stats = new
                {
                    model.TotalUsers,
                    model.TotalServiceProviders,
                    model.TotalShopkeepers,
                    model.TotalPlatformRevenue,
                    model.MonthlyPlatformRevenue,
                    model.PendingCommissionAmount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard data API");
            return Json(new { error = "Failed to load dashboard data" });
        }
    }
    // ---- Activity Log ----
    public async Task<IActionResult> ActivityLog()
    {
        var logs = await _activityLogRepo.Query()
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync();
        return View(logs);
    }

    // ---- Revenue Settings ----
    [HttpGet]
    public async Task<IActionResult> RevenueSettings()
    {
        var rate = await _settingsRepo.FirstOrDefaultAsync(s => s.Key == "CommissionRate");
        ViewBag.CommissionRate = rate?.Value ?? _configuration["GlobalSettings:CommissionRate"] ?? "5";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevenueSettings(string rate)
    {
        var setting = await _settingsRepo.FirstOrDefaultAsync(s => s.Key == "CommissionRate");
        if (setting == null)
        {
            setting = new GlobalSetting { Key = "CommissionRate", Value = rate };
            await _settingsRepo.AddAsync(setting);
        }
        else
        {
            setting.Value = rate;
            setting.UpdatedAt = DateTime.UtcNow;
            _settingsRepo.Update(setting);
        }
        await _settingsRepo.SaveChangesAsync();
        TempData["Success"] = "Commission rate updated successfully!";
        return RedirectToAction("RevenueSettings");
    }

    // ---- Dispute Resolution (Ticket System) ----
    public async Task<IActionResult> Tickets(string? status = null)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var query = db.SupportTickets.Include(t => t.User).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var s))
            query = query.Where(t => t.Status == s);

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return View(tickets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveTicket(int id, string response)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var ticket = await db.SupportTickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.AdminResponse = response;
        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Ticket resolved!";
        return RedirectToAction("Tickets");
    }

    // ---- Vendor Revenue with Filters ----
    public async Task<IActionResult> VendorRevenue(string? vendorId = null, int? month = null, int? year = null)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var query = db.Commissions.Include(c => c.User).AsQueryable();

        if (!string.IsNullOrEmpty(vendorId))
            query = query.Where(c => c.UserId == vendorId);
        if (month.HasValue)
            query = query.Where(c => c.Month == month.Value);
        if (year.HasValue)
            query = query.Where(c => c.Year == year.Value);

        var data = await query.OrderByDescending(c => c.Year).ThenByDescending(c => c.Month).ToListAsync();

        // Populate vendor dropdown
        var providers = await _userManager.GetUsersInRoleAsync("ServiceProvider");
        var shopkeepers = await _userManager.GetUsersInRoleAsync("Shopkeeper");
        ViewBag.Vendors = providers.Concat(shopkeepers).Select(u => new { u.Id, u.FullName }).ToList();
        ViewBag.SelectedVendor = vendorId;
        ViewBag.SelectedMonth = month;
        ViewBag.SelectedYear = year;

        return View(data);
    }

    // ---- Provider Verification ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyProvider(string providerId, bool approve)
    {
        var user = await _userManager.FindByIdAsync(providerId);
        if (user == null) return NotFound();

        user.IsVerifiedExpert = approve;
        await _userManager.UpdateAsync(user);
        TempData["Success"] = approve ? "Provider verified!" : "Verification revoked.";
        return RedirectToAction("Users", new { role = "ServiceProvider" });
    }
}
