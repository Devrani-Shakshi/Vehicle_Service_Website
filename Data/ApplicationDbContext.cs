using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServicePlatform.Models;

namespace ServicePlatform.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
    public DbSet<ChargingStation> ChargingStations => Set<ChargingStation>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<UserVehicle> UserVehicles => Set<UserVehicle>();
    public DbSet<SubTechnician> SubTechnicians => Set<SubTechnician>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // LoginHistory configuration
        builder.Entity<LoginHistory>(entity =>
        {
            entity.HasQueryFilter(e => e.User != null && !e.User.IsDeleted);
        });

        // OtpVerification configuration
        builder.Entity<OtpVerification>(entity =>
        {
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.OtpCode);
        });

        // ServiceRequest configuration
        builder.Entity<ServiceRequest>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ServiceRequestsAsUser)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ServiceProvider)
                .WithMany(u => u.ServiceRequestsAsProvider)
                .HasForeignKey(e => e.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VehicleModel)
                .WithMany(v => v.ServiceRequests)
                .HasForeignKey(e => e.VehicleModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // VehicleModel configuration
        builder.Entity<VehicleModel>(entity =>
        {
            entity.HasIndex(e => e.ModelNumber).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.ServiceProvider)
                .WithMany(u => u.VehicleModels)
                .HasForeignKey(e => e.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Appointment configuration
        builder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(e => e.ScheduledDate);
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AppointmentsAsUser)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ServiceProvider)
                .WithMany(u => u.AppointmentsAsProvider)
                .HasForeignKey(e => e.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Product configuration
        builder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.Shopkeeper)
                .WithMany(u => u.Products)
                .HasForeignKey(e => e.ShopkeeperId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CartItem configuration
        builder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasQueryFilter(e => e.Product != null && !e.Product.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Order configuration
        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem configuration
        builder.Entity<OrderItem>(entity =>
        {
            entity.HasQueryFilter(e => e.Order != null && !e.Order.IsDeleted && e.Product != null && !e.Product.IsDeleted);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Payment configuration
        builder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.Status);
            entity.HasQueryFilter(e => e.User != null && !e.User.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(e => e.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ServiceRequest)
                .WithOne(sr => sr.Payment)
                .HasForeignKey<Payment>(e => e.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Feedback configuration
        builder.Entity<Feedback>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Feedbacks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Rating configuration
        builder.Entity<Rating>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ServiceProvider)
                .WithMany(u => u.RatingsReceived)
                .HasForeignKey(e => e.ServiceProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ServiceRequest)
                .WithMany(sr => sr.Ratings)
                .HasForeignKey(e => e.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasQueryFilter(e => e.User != null && !e.User.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed EV service categories
        builder.Entity<ServiceCategory>().HasData(
            new ServiceCategory { Id = 1, Name = "Battery Service", Icon = "fas fa-car-battery", Description = "EV battery diagnostics, repair and replacement" },
            new ServiceCategory { Id = 2, Name = "Motor Repair", Icon = "fas fa-cogs", Description = "Electric motor diagnostics and repair" },
            new ServiceCategory { Id = 3, Name = "Charging System", Icon = "fas fa-charging-station", Description = "Charging port, cable and system repair" },
            new ServiceCategory { Id = 4, Name = "Tyre & Wheel", Icon = "fas fa-tire", Description = "Tyre replacement, alignment and balancing" },
            new ServiceCategory { Id = 5, Name = "Brake System", Icon = "fas fa-brake", Description = "Brake pad, disc and regenerative braking repair" },
            new ServiceCategory { Id = 6, Name = "Controller / ECU", Icon = "fas fa-microchip", Description = "Electronic control unit and controller repair" },
            new ServiceCategory { Id = 7, Name = "Body & Paint", Icon = "fas fa-spray-can", Description = "Body repair, dent removal and painting" },
            new ServiceCategory { Id = 8, Name = "General Servicing", Icon = "fas fa-tools", Description = "Routine EV maintenance and general servicing" }
        );
    }
}
