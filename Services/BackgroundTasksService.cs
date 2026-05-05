using ServicePlatform.Data;
using ServicePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace ServicePlatform.Services;

public class BackgroundTasksService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTasksService> _logger;

    public BackgroundTasksService(IServiceProvider serviceProvider, ILogger<BackgroundTasksService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Task Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    // Task 1: Auto-complete old appointments (Simulated)
                    var oldAppointments = await dbContext.Appointments
                        .Where(a => a.Status == AppointmentStatus.Scheduled && a.ScheduledDate < DateTime.UtcNow.AddHours(-24))
                        .ToListAsync();

                    foreach (var app in oldAppointments)
                    {
                        app.Status = AppointmentStatus.Completed;
                        _logger.LogInformation("Background Task: Auto-completed appointment {Id}", app.Id);
                    }

                    if (oldAppointments.Any()) await dbContext.SaveChangesAsync();

                    // Task 2: Simulation of Service Reminders
                    _logger.LogInformation("Background Task: Running hourly service reminder check...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background task.");
            }

            // Wait for 1 hour (simulated as 1 minute for demo purposes)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
