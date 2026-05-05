using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ServicePlatform.Models;

namespace ServicePlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("Authenticated user {User} redirected from Home", User.Identity.Name);
                    if (User.IsInRole("Admin"))
                        return RedirectToAction("Index", "Admin");
                    if (User.IsInRole("ServiceProvider"))
                        return RedirectToAction("Index", "ServiceProvider");
                    if (User.IsInRole("Shopkeeper"))
                        return RedirectToAction("Index", "Shopkeeper");
                    return RedirectToAction("Index", "UserDashboard");
                }

                _logger.LogInformation("Anonymous user accessed home page");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Home/Index");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> FindProviders(string? specialization = null, string? certification = null, bool verifiedOnly = false)
        {
            var providers = await _userManager.GetUsersInRoleAsync("ServiceProvider");
            var query = providers.AsEnumerable();

            if (!string.IsNullOrEmpty(specialization))
                query = query.Where(p => p.Specializations != null && p.Specializations.Contains(specialization, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(certification))
                query = query.Where(p => p.Certifications != null && p.Certifications.Contains(certification, StringComparison.OrdinalIgnoreCase));

            if (verifiedOnly)
                query = query.Where(p => p.IsVerifiedExpert);

            return View(query.ToList());
        }

        public IActionResult Error(string? message = null)
        {
            _logger.LogWarning("Error page displayed: {Message}", message ?? "Unknown error");
            ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
            return View();
        }
    }
}
