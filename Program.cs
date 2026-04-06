using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using ServicePlatform.Data;
using ServicePlatform.Hubs;
using ServicePlatform.Middleware;
using ServicePlatform.Models;
using ServicePlatform.Repositories;
using ServicePlatform.Repositories.Interfaces;
using ServicePlatform.Services;
using ServicePlatform.Services.Interfaces;

// Configure NLog
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Application starting up...");

    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add DbContext
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
               .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

    // Add Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Configure cookies
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    // Add session
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // Register repositories
    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

    // Register services
    builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
    builder.Services.AddScoped<IAppointmentService, AppointmentService>();
    builder.Services.AddScoped<IRatingService, RatingService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IPaymentService, PaymentService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IFeedbackService, FeedbackService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();

    // Add SignalR
    builder.Services.AddSignalR();

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
        options.AddPolicy("UserOnly", p => p.RequireRole("User"));
        options.AddPolicy("ServiceProviderOnly", p => p.RequireRole("ServiceProvider"));
        options.AddPolicy("ShopkeeperOnly", p => p.RequireRole("Shopkeeper"));
    });

    // Add caching
    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCaching();

    builder.Services.AddControllersWithViews();

    // Add Antiforgery
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
    });

    var app = builder.Build();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Ensure LoginHistories table since it was added after EnsureCreated runs initially
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LoginHistories' and xtype='U')
            BEGIN
                CREATE TABLE [LoginHistories] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] nvarchar(450) NOT NULL,
                    [LoginTime] datetime2 NOT NULL,
                    [IpAddress] nvarchar(50) NULL,
                    [UserAgent] nvarchar(500) NULL,
                    CONSTRAINT [PK_LoginHistories] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_LoginHistories_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
            END
        ";
        await context.Database.ExecuteSqlRawAsync(sql);

        await DbSeeder.SeedRolesAndAdmin(scope.ServiceProvider);
        await DbSeeder.SeedDemoData(scope.ServiceProvider);
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseResponseCaching();
    app.UseSession();

    // Custom middleware
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    // SignalR hub
    app.MapHub<NotificationHub>("/hubs/notification");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    logger.Info("Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
