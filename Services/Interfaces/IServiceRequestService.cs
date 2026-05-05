using ServicePlatform.Models;
using ServicePlatform.ViewModels.Service;

namespace ServicePlatform.Services.Interfaces;

public interface IServiceRequestService
{
    /// <summary>
    /// Creates a new service request for a user.
    /// </summary>
    /// <param name="model">The request details.</param>
    /// <param name="userId">The ID of the user creating the request.</param>
    /// <returns>The created ServiceRequest entity.</returns>
    Task<ServiceRequest> CreateRequestAsync(ServiceRequestViewModel model, string userId);

    /// <summary>
    /// Retrieves all service requests for a specific user.
    /// </summary>
    Task<IEnumerable<ServiceRequest>> GetUserRequestsAsync(string userId);

    /// <summary>
    /// Retrieves service requests for a provider, optionally filtered by status.
    /// </summary>
    Task<IEnumerable<ServiceRequest>> GetProviderRequestsAsync(string? providerId = null, ServiceRequestStatus? status = null);

    /// <summary>
    /// Retrieves a specific service request by ID, with optional ownership verification.
    /// </summary>
    /// <param name="id">The request ID.</param>
    /// <param name="userId">If provided, ensures the request belongs to or is assigned to this user.</param>
    Task<ServiceRequest?> GetByIdAsync(int id, string? userId = null);
    Task<bool> AcceptRequestAsync(int id, string providerId);
    Task<bool> RejectRequestAsync(int id, string providerId);
    Task<bool> UpdateStatusAsync(int id, ServiceRequestStatus status);
    Task<bool> CompleteRequestAsync(int id);
    Task<int> GetCountByStatusAsync(ServiceRequestStatus status, string? userId = null);
    Task<IEnumerable<ServiceRequest>> GetAllRequestsAsync(int page = 1, int pageSize = 10);
    Task<bool> DeleteRequestAsync(int id, string userId);
    Task<int> GetVehicleHealthScoreAsync(string userId);
    Task<int> GetTotalCountAsync();
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
