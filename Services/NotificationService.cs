using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServicePlatform.Hubs;
using ServicePlatform.Models;
using ServicePlatform.Repositories.Interfaces;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Services;

public class NotificationService : INotificationService
{
    private readonly IGenericRepository<Notification> _repository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(IGenericRepository<Notification> repository, IHubContext<NotificationHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? link = null, object? extraData = null)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            UserId = userId
        };

        await _repository.AddAsync(notification);
        await _repository.SaveChangesAsync();

        // Push real-time notification via SignalR
        var signalRPayload = new Dictionary<string, object?>
        {
            { "id", notification.Id },
            { "title", notification.Title },
            { "message", notification.Message },
            { "type", notification.Type.ToString() },
            { "link", notification.Link },
            { "createdAt", notification.CreatedAt.ToString("MMM dd, yyyy HH:mm") },
            { "isRead", notification.IsRead }
        };

        if (extraData != null)
        {
            var properties = extraData.GetType().GetProperties();
            foreach (var prop in properties)
            {
                signalRPayload[prop.Name.ToLower()] = prop.GetValue(extraData);
            }
        }

        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", signalRPayload);
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int count = 20) =>
        await _repository.Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(string userId) =>
        await _repository.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            _repository.Update(notification);
            await _repository.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _repository.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var n in notifications)
        {
            n.IsRead = true;
            _repository.Update(n);
        }
        await _repository.SaveChangesAsync();
    }
}

public class FeedbackService : IFeedbackService
{
    private readonly IGenericRepository<Feedback> _repository;

    public FeedbackService(IGenericRepository<Feedback> repository) => _repository = repository;

    public async Task<Feedback> CreateFeedbackAsync(string userId, string message, int rating, string? subject = null, string? category = null)
    {
        var feedback = new Feedback
        {
            Subject = subject ?? "User Feedback",
            Message = message,
            Rating = rating,
            Category = category,
            UserId = userId
        };

        await _repository.AddAsync(feedback);
        await _repository.SaveChangesAsync();
        return feedback;
    }

    public async Task<IEnumerable<Feedback>> GetUserFeedbacksAsync(string userId) =>
        await _repository.Query()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Feedback>> GetAllFeedbacksAsync() =>
        await _repository.Query()
            .Include(f => f.User)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<bool> ResolveFeedbackAsync(int id, string adminResponse)
    {
        var feedback = await _repository.GetByIdAsync(id);
        if (feedback == null) return false;

        feedback.IsResolved = true;
        feedback.AdminResponse = adminResponse;
        feedback.ResolvedAt = DateTime.UtcNow;
        _repository.Update(feedback);
        await _repository.SaveChangesAsync();
        return true;
    }
}
