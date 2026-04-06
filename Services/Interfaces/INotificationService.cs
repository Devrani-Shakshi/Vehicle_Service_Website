using ServicePlatform.Models;

namespace ServicePlatform.Services.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? link = null);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int count = 20);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
}

public interface IFeedbackService
{
    Task<Feedback> CreateFeedbackAsync(string userId, string subject, string message, string? category = null);
    Task<IEnumerable<Feedback>> GetUserFeedbacksAsync(string userId);
    Task<IEnumerable<Feedback>> GetAllFeedbacksAsync();
    Task<bool> ResolveFeedbackAsync(int id, string adminResponse);
}

public interface IDashboardService
{
    Task<ViewModels.Admin.AdminDashboardViewModel> GetAdminDashboardAsync();
    Task<ViewModels.Service.UserDashboardViewModel> GetUserDashboardAsync(string userId);
    Task<ViewModels.Service.ServiceProviderDashboardViewModel> GetProviderDashboardAsync(string providerId);
    Task<ViewModels.Shop.ShopkeeperDashboardViewModel> GetShopkeeperDashboardAsync(string shopkeeperId);
}
