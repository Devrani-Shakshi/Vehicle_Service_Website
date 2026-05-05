using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServicePlatform.Helpers;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Account;
using ServicePlatform.Data;
using ServicePlatform.Repositories.Interfaces;

namespace ServicePlatform.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;
    private readonly ILoginHistoryRepository _loginHistoryRepo;
    private readonly IOtpService _otpService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailService emailService,
        ILogger<AccountController> logger,
        ILoginHistoryRepository loginHistoryRepo,
        IOtpService otpService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _logger = logger;
        _loginHistoryRepo = loginHistoryRepo;
        _otpService = otpService;
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

        // Server-side State whitelist validation
        if (!AppConstants.AllStates.Contains(model.State))
        {
            ModelState.AddModelError("State", "Invalid state selected.");
            return View(model);
        }

        if (model.Email != null) model.Email = model.Email.ToLower().Trim();

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
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                var validRoles = new[] { "User", "ServiceProvider", "Shopkeeper" };
                var role = validRoles.Contains(model.Role) ? model.Role : "User";

                await _userManager.AddToRoleAsync(user, role);
                _logger.LogInformation("User {Email} registered with role {Role}", model.Email, role);

                TempData["Success"] = "Account created successfully! Please sign in with your credentials.";
                return RedirectToAction("Login");
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
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        if (string.IsNullOrEmpty(model.Email))
        {
            ModelState.AddModelError(string.Empty, "Email is required.");
            return View(model);
        }

        model.Email = model.Email.ToLower().Trim();

        try
        {
            // Check if user exists first
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "User not found. Please register yourself first.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} logged in successfully", model.Email);
                var loginHistory = new LoginHistory
                {
                    UserId = user.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };
                await _loginHistoryRepo.AddAsync(loginHistory);
                await _loginHistoryRepo.SaveChangesAsync();

                var roles = await _userManager.GetRolesAsync(user);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToDashboard(roles.FirstOrDefault());
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
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            
            var otp = await _otpService.GenerateOtpAsync(model.Email);

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
                        <h1 style='color:#4f46e5;margin:0;font-size:24px;'>EV ServicePlatform</h1>
                    </div>
                    <h2 style='font-size:20px;font-weight:600;margin-bottom:16px;color:#111827;'>Password Reset Request</h2>
                    <p style='line-height:1.6;margin-bottom:24px;'>Hi {user.FullName},</p>
                    <p style='line-height:1.6;margin-bottom:24px;'>We received a request to reset your password. Use the following 6-digit verification code to proceed:</p>
                    <div style='background-color:#f3f4f6;padding:24px;text-align:center;border-radius:8px;margin-bottom:24px;'>
                        <span style='font-size:32px;font-weight:700;letter-spacing:8px;color:#4f46e5;'>{otp}</span>
                    </div>
                    <p style='font-size:14px;color:#6b7280;line-height:1.6;margin-bottom:24px;'>This code will expire in 10 minutes. If you did not request a password reset, you can safely ignore this email.</p>
                    <hr style='border:0;border-top:1px solid #e5e7eb;margin-bottom:24px;' />
                    <p style='font-size:12px;color:#9ca3af;text-align:center;margin:0;'>&copy; {DateTime.Now.Year} EV ServicePlatform. All rights reserved.</p>
                </div>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, "EV ServicePlatform — Password Reset OTP", emailBody);
                TempData["Success"] = "An OTP has been sent to your registered email.";
                
                // Only show demo OTP if service is not configured
                if (!_emailService.IsConfigured)
                {
                    TempData["DemoOTP"] = otp;
                }
            }
            catch
            {
                // If email fails, use session based demo fall-back
                _logger.LogWarning("Email sending failed for {Email}, OTP stored in session for demo", model.Email);
                TempData["Success"] = "Unable to deliver email. Using demo mode.";
                TempData["DemoOTP"] = otp;
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
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var isValid = await _otpService.VerifyOtpAsync(model.Email, model.Otp);

            if (!isValid)
            {
                _logger.LogWarning("Invalid or expired OTP entered for {Email}", model.Email);
                ModelState.AddModelError("Otp", "Invalid or expired OTP. Please try again.");
                return View(model);
            }

            _logger.LogInformation("OTP verified successfully for {Email}", model.Email);
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
    public async Task<IActionResult> ResetPassword(string email)
    {
        if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");

        var verified = await _otpService.IsVerifiedAsync(email);
        if (!verified)
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
            var verified = await _otpService.IsVerifiedAsync(model.Email);
            if (!verified)
            {
                TempData["Error"] = "Verification expired. Please start over.";
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
                await _otpService.ClearVerificationAsync(model.Email);
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

    // ── Change Password Feature ──────────────────────────────

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login");

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User {Email} changed their password successfully", user.Email);
            TempData["Success"] = "Your password has been changed successfully!";
            return RedirectToDashboard();
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }
}
