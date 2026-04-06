using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    private string UserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var notifications = await _notificationService.GetUserNotificationsAsync(UserId);
            var unreadCount = await _notificationService.GetUnreadCountAsync(UserId);
            _logger.LogInformation("User {UserId} fetched notifications (unread: {Count})", UserId, unreadCount);
            return Json(new { notifications, unreadCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications for {UserId}", UserId);
            return Json(new { notifications = Array.Empty<object>(), unreadCount = 0 });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            _logger.LogInformation("User {UserId} marking notification {NotificationId} as read", UserId, id);
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return Json(new { success = false });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            _logger.LogInformation("User {UserId} marking all notifications as read", UserId);
            await _notificationService.MarkAllAsReadAsync(UserId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for {UserId}", UserId);
            return Json(new { success = false });
        }
    }
}
