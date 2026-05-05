using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ServicePlatform.Controllers;

[Authorize]
public class DiagnosticController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
