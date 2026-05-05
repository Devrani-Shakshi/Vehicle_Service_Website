using ServicePlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace ServicePlatform.ViewModels.Service;

public class ServiceRequestViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 2000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Request type is required")]
    public ServiceRequestType RequestType { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Location address is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Location address must be between 5 and 500 characters")]
    public string LocationAddress { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Range(0, 100, ErrorMessage = "Battery SOH must be between 0 and 100%")]
    [Display(Name = "Battery State of Health (%)")]
    public int? BatteryStateOfHealth { get; set; }

    [Display(Name = "Kilometers Driven")]
    public long? KilometersDriven { get; set; }

    [MaxLength(100)]
    [Display(Name = "Vehicle Model")]
    public string? VehicleModelName { get; set; }

    public int? VehicleModelId { get; set; }
}

public class AppointmentViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Appointment date and time is required")]
    [DataType(DataType.DateTime)]
    [FutureDate(ErrorMessage = "Appointment date must be today or a future date")]
    public DateTime ScheduledDate { get; set; } = DateTime.Now.AddDays(1);

    [MaxLength(100)]
    public string? TimeSlot { get; set; }

    public string? ServiceProviderId { get; set; }
}

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime date)
        {
            return date.Date >= DateTime.Today;
        }
        return true;
    }
}

public class UserDashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public int PendingRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int TotalOrders { get; set; }
    public int UpcomingAppointments { get; set; }
    public List<ServiceRequest> RecentRequests { get; set; } = new();
    public List<Appointment> UpcomingAppointmentsList { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();

    // Analytics
    public List<BatteryHealthPoint> BatteryHealthHistory { get; set; } = new();
    public int VehicleHealthScore { get; set; }
}

public class BatteryHealthPoint
{
    public string Date { get; set; } = string.Empty;
    public int Health { get; set; }
}

public class ServiceProviderDashboardViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public int PendingRequests { get; set; }
    public int AcceptedRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int TotalEarnings { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<ServiceRequest> PendingRequestsList { get; set; } = new();
    public List<ServiceRequest> ActiveRequests { get; set; } = new();
    public List<Rating> RecentRatings { get; set; } = new();

    // Analytics
    public List<ServiceCategoryStat> CategoryStats { get; set; } = new();
    public List<MonthlyRevenuePoint> RevenueTrend { get; set; } = new();
}

public class ServiceCategoryStat
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyRevenuePoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class RatingViewModel
{
    [Required]
    public int ServiceRequestId { get; set; }

    [Required]
    public string ServiceProviderId { get; set; } = string.Empty;

    [Required, Range(1, 5)]
    public int Stars { get; set; }

    [MaxLength(1000)]
    public string? ReviewText { get; set; }
}
