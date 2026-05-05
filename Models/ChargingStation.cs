using System.ComponentModel.DataAnnotations;

namespace ServicePlatform.Models;

public class ChargingStation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string ConnectorTypes { get; set; } = "Type 2, CCS"; // e.g., "Type 2, CCS, CHAdeMO"
    public string PowerOutput { get; set; } = "50kW";

    // Real-time Status (Crowdsourced)
    public string CurrentStatus { get; set; } = "Working"; // "Working", "Out of Order", "Under Maintenance"
    public int QueueCount { get; set; } = 0;
    public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
