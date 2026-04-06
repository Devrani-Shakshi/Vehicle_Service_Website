using Microsoft.AspNetCore.Mvc;

namespace ServicePlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
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

        public IActionResult Error(string? message = null)
        {
            _logger.LogWarning("Error page displayed: {Message}", message ?? "Unknown error");
            ViewBag.ErrorMessage = message ?? "An unexpected error occurred.";
            return View();
        }
    }
}
