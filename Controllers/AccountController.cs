using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServicePlatform.Helpers;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Account;
using ServicePlatform.Data;

namespace ServicePlatform.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;
    private readonly ApplicationDbContext _context;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        ILogger<AccountController> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _logger = logger;
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        ViewBag.States = AppConstants.AllStates;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewBag.States = AppConstants.AllStates;
        if (!ModelState.IsValid) return View(model);

        try
        {
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
                var validRoles = new[] { "User", "ServiceProvider", "Shopkeeper" };
                var role = validRoles.Contains(model.Role) ? model.Role : "User";

                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("User {Email} registered with role {Role}", model.Email, role);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToDashboard(role);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", model.Email);
            TempData["Error"] = "An unexpected error occurred during registration. Please try again.";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", model.Email);
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var loginHistory = new LoginHistory
                    {
                        UserId = user.Id,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString()
                    };
                    _context.LoginHistories.Add(loginHistory);
                    await _context.SaveChangesAsync();

                    var roles = await _userManager.GetRolesAsync(user);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToDashboard(roles.FirstOrDefault());
                }
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked out", model.Email);
                ModelState.AddModelError(string.Empty, "Account is locked out. Please try again later.");
                return View(model);
            }

            _logger.LogWarning("Failed login attempt for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", model.Email);
            TempData["Error"] = "An unexpected error occurred during login. Please try again.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var email = User.Identity?.Name;
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Email} logged out", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }

        return RedirectToAction("Login");
    }

    // ── Forgot Password Flow ─────────────────────────────────

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            
            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store OTP in session
            HttpContext.Session.SetString($"OTP_{model.Email}", otp);
            HttpContext.Session.SetString($"OTP_EXPIRY_{model.Email}", DateTime.UtcNow.AddMinutes(10).ToString("o"));

            if (user == null)
            {
                // Don't reveal that the user does not exist
                _logger.LogWarning("Forgot password attempt for non-existent email {Email}", model.Email);
                TempData["Success"] = $"If an account with that email exists, an OTP has been sent. (Demo OTP: {otp})";
                return RedirectToAction("VerifyOtp", new { email = model.Email });
            }

            _logger.LogInformation("OTP generated for password reset for {Email}", model.Email);

            // Send OTP email
            var emailBody = $@"
                <div style='font-family:Segoe UI,Tahoma,Geneva,Verdana,sans-serif;max-width:500px;margin:0 auto;padding:40px;background-color:#ffffff;border:1px solid #e5e7eb;border-radius:12px;color:#1f2937;'>
                    <div style='text-align:center;margin-bottom:30px;'>
                        <h1 style='color:#4f46e5;margin:0;font-size:24px;'>ServicePlatform</h1>
                    </div>
                    <h2 style='font-size:20px;font-weight:600;margin-bottom:16px;color:#111827;'>Password Reset Request</h2>
                    <p style='line-height:1.6;margin-bottom:24px;'>Hi {user.FullName},</p>
                    <p style='line-height:1.6;margin-bottom:24px;'>We received a request to reset your password. Use the following 6-digit verification code to proceed:</p>
                    <div style='background-color:#f3f4f6;padding:24px;text-align:center;border-radius:8px;margin-bottom:24px;'>
                        <span style='font-size:32px;font-weight:700;letter-spacing:8px;color:#4f46e5;'>{otp}</span>
                    </div>
                    <p style='font-size:14px;color:#6b7280;line-height:1.6;margin-bottom:24px;'>This code will expire in 10 minutes. If you did not request a password reset, you can safely ignore this email.</p>
                    <hr style='border:0;border-top:1px solid #e5e7eb;margin-bottom:24px;' />
                    <p style='font-size:12px;color:#9ca3af;text-align:center;margin:0;'>&copy; {DateTime.Now.Year} ServicePlatform. All rights reserved.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, "ServicePlatform — Password Reset OTP", emailBody);
                TempData["Success"] = $"An OTP has been sent to your registered email. (Demo OTP: {otp})";
            }
            catch
            {
                // If email fails, still redirect (OTP is in session for demo)
                _logger.LogWarning("Email sending failed for {Email}, OTP stored in session for demo", model.Email);
                TempData["Success"] = $"Email service unavailable. For demo, your OTP is: {otp}";
            }

            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", model.Email);
            TempData["Error"] = "An unexpected error occurred. Please try again.";
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
        return View(new VerifyOtpViewModel { Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var storedOtp = HttpContext.Session.GetString($"OTP_{model.Email}");
            var expiryStr = HttpContext.Session.GetString($"OTP_EXPIRY_{model.Email}");

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(expiryStr))
            {
                _logger.LogWarning("OTP verification attempt with no stored OTP for {Email}", model.Email);
                TempData["Error"] = "OTP has expired. Please request a new one.";
                return RedirectToAction("ForgotPassword");
            }

            var expiry = DateTime.Parse(expiryStr);
            if (DateTime.UtcNow > expiry)
            {
                _logger.LogWarning("Expired OTP used for {Email}", model.Email);
                HttpContext.Session.Remove($"OTP_{model.Email}");
                HttpContext.Session.Remove($"OTP_EXPIRY_{model.Email}");
                TempData["Error"] = "OTP has expired. Please request a new one.";
                return RedirectToAction("ForgotPassword");
            }

            if (storedOtp != model.Otp)
            {
                _logger.LogWarning("Invalid OTP entered for {Email}", model.Email);
                ModelState.AddModelError("Otp", "Invalid OTP. Please try again.");
                return View(model);
            }

            // OTP verified — allow password reset
            _logger.LogInformation("OTP verified successfully for {Email}", model.Email);
            HttpContext.Session.SetString($"OTP_VERIFIED_{model.Email}", "true");
            HttpContext.Session.Remove($"OTP_{model.Email}");
            HttpContext.Session.Remove($"OTP_EXPIRY_{model.Email}");

            return RedirectToAction("ResetPassword", new { email = model.Email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OTP verification for {Email}", model.Email);
            TempData["Error"] = "An unexpected error occurred. Please try again.";
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult ResetPassword(string email)
    {
        if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");

        var verified = HttpContext.Session.GetString($"OTP_VERIFIED_{email}");
        if (verified != "true")
        {
            TempData["Error"] = "Please verify the OTP first.";
            return RedirectToAction("ForgotPassword");
        }

        return View(new ResetPasswordViewModel { Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var verified = HttpContext.Session.GetString($"OTP_VERIFIED_{model.Email}");
            if (verified != "true")
            {
                TempData["Error"] = "Session expired. Please start over.";
                return RedirectToAction("ForgotPassword");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "Unable to reset password. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            // Reset password using token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for {Email}", model.Email);
                HttpContext.Session.Remove($"OTP_VERIFIED_{model.Email}");
                TempData["Success"] = "Password has been reset successfully! Please sign in with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            _logger.LogWarning("Password reset failed for {Email}: {Errors}",
                model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for {Email}", model.Email);
            TempData["Error"] = "An unexpected error occurred. Please try again.";
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToDashboard(string? role = null)
    {
        if (role == null && User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin")) role = "Admin";
            else if (User.IsInRole("ServiceProvider")) role = "ServiceProvider";
            else if (User.IsInRole("Shopkeeper")) role = "Shopkeeper";
            else role = "User";
        }

        return role switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "ServiceProvider" => RedirectToAction("Index", "ServiceProvider"),
            "Shopkeeper" => RedirectToAction("Index", "Shopkeeper"),
            _ => RedirectToAction("Index", "UserDashboard")
        };
    }
}
