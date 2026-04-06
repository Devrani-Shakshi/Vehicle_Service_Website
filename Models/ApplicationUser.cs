using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ServicePlatform.Models;

public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(15)]
    public string? Mobile { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<ServiceRequest> ServiceRequestsAsUser { get; set; } = new List<ServiceRequest>();
    public virtual ICollection<ServiceRequest> ServiceRequestsAsProvider { get; set; } = new List<ServiceRequest>();
    public virtual ICollection<Appointment> AppointmentsAsUser { get; set; } = new List<Appointment>();
    public virtual ICollection<Appointment> AppointmentsAsProvider { get; set; } = new List<Appointment>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public virtual ICollection<Rating> RatingsGiven { get; set; } = new List<Rating>();
    public virtual ICollection<Rating> RatingsReceived { get; set; } = new List<Rating>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
