using ServicePlatform.Models;
using ServicePlatform.ViewModels.Service;

namespace ServicePlatform.Services.Interfaces;

public interface IServiceRequestService
{
    Task<ServiceRequest> CreateRequestAsync(ServiceRequestViewModel model, string userId);
    Task<IEnumerable<ServiceRequest>> GetUserRequestsAsync(string userId);
    Task<IEnumerable<ServiceRequest>> GetProviderRequestsAsync(string? providerId = null, ServiceRequestStatus? status = null);
    Task<ServiceRequest?> GetByIdAsync(int id);
    Task<bool> AcceptRequestAsync(int id, string providerId);
    Task<bool> RejectRequestAsync(int id, string providerId);
    Task<bool> UpdateStatusAsync(int id, ServiceRequestStatus status);
    Task<bool> CompleteRequestAsync(int id);
    Task<int> GetCountByStatusAsync(ServiceRequestStatus status, string? userId = null);
    Task<IEnumerable<ServiceRequest>> GetAllRequestsAsync();
}

public interface IAppointmentService
{
    Task<Appointment> CreateAppointmentAsync(AppointmentViewModel model, string userId);
    Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(string userId);
    Task<IEnumerable<Appointment>> GetProviderAppointmentsAsync(string providerId);
    Task<bool> UpdateStatusAsync(int id, AppointmentStatus status);
    Task<Appointment?> GetByIdAsync(int id);
}

public interface IRatingService
{
    Task<Rating> CreateRatingAsync(RatingViewModel model, string userId);
    Task<IEnumerable<Rating>> GetProviderRatingsAsync(string providerId);
    Task<double> GetProviderAverageRatingAsync(string providerId);
    Task<int> GetProviderReviewCountAsync(string providerId);
}
