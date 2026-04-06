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
            entity.HasQueryFilter(e => !e.User!.IsDeleted);
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
            entity.HasQueryFilter(e => !e.Product!.IsDeleted);

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
            entity.HasQueryFilter(e => !e.Order!.IsDeleted && !e.Product!.IsDeleted);

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
            entity.HasQueryFilter(e => !e.User!.IsDeleted);

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
            entity.HasQueryFilter(e => !e.User!.IsDeleted);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed service categories
        builder.Entity<ServiceCategory>().HasData(
            new ServiceCategory { Id = 1, Name = "Plumbing", Icon = "fas fa-wrench", Description = "Plumbing and pipe repair services" },
            new ServiceCategory { Id = 2, Name = "Electrical", Icon = "fas fa-bolt", Description = "Electrical repair and installation" },
            new ServiceCategory { Id = 3, Name = "Carpentry", Icon = "fas fa-hammer", Description = "Woodwork and furniture repair" },
            new ServiceCategory { Id = 4, Name = "Cleaning", Icon = "fas fa-broom", Description = "Home and office cleaning services" },
            new ServiceCategory { Id = 5, Name = "Painting", Icon = "fas fa-paint-roller", Description = "Interior and exterior painting" },
            new ServiceCategory { Id = 6, Name = "HVAC", Icon = "fas fa-fan", Description = "Heating, ventilation and air conditioning" },
            new ServiceCategory { Id = 7, Name = "Appliance Repair", Icon = "fas fa-tools", Description = "Home appliance repair services" },
            new ServiceCategory { Id = 8, Name = "Pest Control", Icon = "fas fa-bug", Description = "Pest control and fumigation" }
        );
    }
}
