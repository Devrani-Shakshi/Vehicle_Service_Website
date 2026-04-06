using Microsoft.EntityFrameworkCore;
using ServicePlatform.Models;
using ServicePlatform.Repositories.Interfaces;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Service;

namespace ServicePlatform.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IGenericRepository<ServiceRequest> _repository;
    private readonly INotificationService _notificationService;

    public ServiceRequestService(IGenericRepository<ServiceRequest> repository, INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    public async Task<ServiceRequest> CreateRequestAsync(ServiceRequestViewModel model, string userId)
    {
        var request = new ServiceRequest
        {
            Title = model.Title,
            Description = model.Description,
            RequestType = model.RequestType,
            Category = model.Category,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            LocationAddress = model.LocationAddress,
            Notes = model.Notes,
            UserId = userId,
            Status = ServiceRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(request);
        await _repository.SaveChangesAsync();
        return request;
    }

    public async Task<IEnumerable<ServiceRequest>> GetUserRequestsAsync(string userId) =>
        await _repository.Query()
            .Include(r => r.ServiceProvider)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<ServiceRequest>> GetProviderRequestsAsync(string? providerId = null, ServiceRequestStatus? status = null)
    {
        var query = _repository.Query().Include(r => r.User).AsQueryable();

        if (providerId != null)
            query = query.Where(r => r.ServiceProviderId == providerId || r.Status == ServiceRequestStatus.Pending);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<ServiceRequest?> GetByIdAsync(int id) =>
        await _repository.Query()
            .Include(r => r.User)
            .Include(r => r.ServiceProvider)
            .Include(r => r.Ratings)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<bool> AcceptRequestAsync(int id, string providerId)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null || request.Status != ServiceRequestStatus.Pending) return false;

        request.Status = ServiceRequestStatus.Accepted;
        request.ServiceProviderId = providerId;
        request.AcceptedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        _repository.Update(request);
        await _repository.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(request.UserId,
            "Request Accepted", $"Your service request '{request.Title}' has been accepted.",
            NotificationType.RequestAccepted, $"/UserDashboard/RequestDetails/{id}");

        return true;
    }

    public async Task<bool> RejectRequestAsync(int id, string providerId)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return false;

        request.Status = ServiceRequestStatus.Rejected;
        request.UpdatedAt = DateTime.UtcNow;

        _repository.Update(request);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, ServiceRequestStatus status)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return false;

        request.Status = status;
        request.UpdatedAt = DateTime.UtcNow;
        if (status == ServiceRequestStatus.Completed)
            request.CompletedAt = DateTime.UtcNow;

        _repository.Update(request);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteRequestAsync(int id)
    {
        var request = await _repository.GetByIdAsync(id);
        if (request == null) return false;

        request.Status = ServiceRequestStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        _repository.Update(request);
        await _repository.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(request.UserId,
            "Service Completed", $"Your service request '{request.Title}' has been completed.",
            NotificationType.RequestCompleted, $"/UserDashboard/RequestDetails/{id}");

        return true;
    }

    public async Task<int> GetCountByStatusAsync(ServiceRequestStatus status, string? userId = null)
    {
        if (userId != null)
            return await _repository.CountAsync(r => r.Status == status && r.UserId == userId);
        return await _repository.CountAsync(r => r.Status == status);
    }

    public async Task<IEnumerable<ServiceRequest>> GetAllRequestsAsync() =>
        await _repository.Query()
            .Include(r => r.User)
            .Include(r => r.ServiceProvider)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}

public class AppointmentService : IAppointmentService
{
    private readonly IGenericRepository<Appointment> _repository;

    public AppointmentService(IGenericRepository<Appointment> repository) =>
        _repository = repository;

    public async Task<Appointment> CreateAppointmentAsync(AppointmentViewModel model, string userId)
    {
        var appointment = new Appointment
        {
            Title = model.Title,
            Description = model.Description,
            ScheduledDate = model.ScheduledDate,
            TimeSlot = model.TimeSlot,
            ServiceProviderId = model.ServiceProviderId,
            UserId = userId,
            Status = AppointmentStatus.Scheduled
        };

        await _repository.AddAsync(appointment);
        await _repository.SaveChangesAsync();
        return appointment;
    }

    public async Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(string userId) =>
        await _repository.Query()
            .Include(a => a.ServiceProvider)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.ScheduledDate)
            .ToListAsync();

    public async Task<IEnumerable<Appointment>> GetProviderAppointmentsAsync(string providerId) =>
        await _repository.Query()
            .Include(a => a.User)
            .Where(a => a.ServiceProviderId == providerId)
            .OrderByDescending(a => a.ScheduledDate)
            .ToListAsync();

    public async Task<bool> UpdateStatusAsync(int id, AppointmentStatus status)
    {
        var appointment = await _repository.GetByIdAsync(id);
        if (appointment == null) return false;

        appointment.Status = status;
        appointment.UpdatedAt = DateTime.UtcNow;
        _repository.Update(appointment);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<Appointment?> GetByIdAsync(int id) =>
        await _repository.Query()
            .Include(a => a.User)
            .Include(a => a.ServiceProvider)
            .FirstOrDefaultAsync(a => a.Id == id);
}

public class RatingService : IRatingService
{
    private readonly IGenericRepository<Rating> _repository;

    public RatingService(IGenericRepository<Rating> repository) => _repository = repository;

    public async Task<Rating> CreateRatingAsync(RatingViewModel model, string userId)
    {
        var rating = new Rating
        {
            Stars = model.Stars,
            ReviewText = model.ReviewText,
            UserId = userId,
            ServiceProviderId = model.ServiceProviderId,
            ServiceRequestId = model.ServiceRequestId
        };

        await _repository.AddAsync(rating);
        await _repository.SaveChangesAsync();
        return rating;
    }

    public async Task<IEnumerable<Rating>> GetProviderRatingsAsync(string providerId) =>
        await _repository.Query()
            .Include(r => r.User)
            .Where(r => r.ServiceProviderId == providerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<double> GetProviderAverageRatingAsync(string providerId)
    {
        var ratings = await _repository.FindAsync(r => r.ServiceProviderId == providerId);
        return ratings.Any() ? ratings.Average(r => r.Stars) : 0;
    }

    public async Task<int> GetProviderReviewCountAsync(string providerId) =>
        await _repository.CountAsync(r => r.ServiceProviderId == providerId);
}
