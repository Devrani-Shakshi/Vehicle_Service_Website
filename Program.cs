using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        WebRootPath = "wwwroot"
    });

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Production-ready HTTPS enforcement
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
    builder.Services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();

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
    builder.Services.AddScoped<IVehicleService, VehicleService>();
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
    builder.Services.AddScoped<IOtpService, OtpService>();
    builder.Services.AddHostedService<BackgroundTasksService>();

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

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowAnyOrigin(); // Adjust for production specific origins
        });
    });

    // Add Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth", options =>
        {
            options.PermitLimit = 5; // 5 requests
            options.Window = TimeSpan.FromMinutes(1); // per minute
            options.QueueLimit = 0;
        });
    });

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "EV Service Platform API",
            Version = "v1",
            Description = "API for EV Service Platform Management System"
        });
        
        // Include XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (System.IO.File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    });

    // Add Antiforgery
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
    });

    builder.Services.AddSignalR();

    var app = builder.Build();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // await context.Database.EnsureDeletedAsync();
        try {
            await context.Database.MigrateAsync();
        } catch {
            // If migrations fail (e.g. __EFMigrationsHistory missing), EnsureCreated will at least try to build the schema
            await context.Database.EnsureCreatedAsync();
        }

        // Ensure LoginHistories table since it was added after EnsureCreated runs initially
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('LoginHistories') AND type = 'U')
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

        // Manually create tables that were added after the initial migration
        var createTablesSql = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('ActivityLogs') AND type = 'U')
            BEGIN
                CREATE TABLE [ActivityLogs] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] nvarchar(450) NOT NULL,
                    [Action] nvarchar(max) NOT NULL,
                    [Details] nvarchar(max) NOT NULL,
                    [IpAddress] nvarchar(max) NULL,
                    [PerformedBy] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ActivityLogs_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('GlobalSettings') AND type = 'U')
            BEGIN
                CREATE TABLE [GlobalSettings] (
                    [Key] nvarchar(450) NOT NULL,
                    [Value] nvarchar(max) NOT NULL,
                    [Description] nvarchar(max) NULL,
                    [UpdatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_GlobalSettings] PRIMARY KEY ([Key])
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('ChargingStations') AND type = 'U')
            BEGIN
                CREATE TABLE [ChargingStations] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(max) NOT NULL,
                    [Address] nvarchar(max) NOT NULL,
                    [City] nvarchar(max) NOT NULL,
                    [State] nvarchar(max) NOT NULL,
                    [Latitude] float NOT NULL,
                    [Longitude] float NOT NULL,
                    [ConnectorTypes] nvarchar(max) NOT NULL,
                    [PowerOutput] nvarchar(max) NOT NULL,
                    [CurrentStatus] nvarchar(max) NOT NULL,
                    [QueueCount] int NOT NULL,
                    [LastStatusUpdate] datetime2 NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_ChargingStations] PRIMARY KEY ([Id])
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('SupportTickets') AND type = 'U')
            BEGIN
                CREATE TABLE [SupportTickets] (
                    [Id] int NOT NULL IDENTITY,
                    [Subject] nvarchar(200) NOT NULL,
                    [Description] nvarchar(max) NOT NULL,
                    [Status] int NOT NULL,
                    [Category] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [ResolvedAt] datetime2 NULL,
                    [UserId] nvarchar(450) NOT NULL,
                    [OrderId] nvarchar(max) NULL,
                    [ServiceRequestId] int NULL,
                    [AdminResponse] nvarchar(max) NULL,
                    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SupportTickets_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('UserVehicles') AND type = 'U')
            BEGIN
                CREATE TABLE [UserVehicles] (
                    [Id] int NOT NULL IDENTITY,
                    [Make] nvarchar(100) NOT NULL,
                    [Model] nvarchar(100) NOT NULL,
                    [Year] int NOT NULL,
                    [BatteryCapacityKWh] int NOT NULL,
                    [RegistrationNumber] nvarchar(20) NULL,
                    [IsPrimary] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UserId] nvarchar(450) NOT NULL,
                    CONSTRAINT [PK_UserVehicles] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_UserVehicles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('SubTechnicians') AND type = 'U')
            BEGIN
                CREATE TABLE [SubTechnicians] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Mobile] nvarchar(15) NULL,
                    [Specialization] nvarchar(200) NULL,
                    [IsAvailable] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [ProviderId] nvarchar(450) NOT NULL,
                    CONSTRAINT [PK_SubTechnicians] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SubTechnicians_AspNetUsers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
            END
        ";
        await context.Database.ExecuteSqlRawAsync(createTablesSql);

        // Manually add RowVersion and other missing columns if they are missing
        var patchSql = @"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Products') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'RowVersion')
            BEGIN
                ALTER TABLE [Products] ADD [RowVersion] rowversion NOT NULL;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Products') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CompatibleVehicleModels')
            BEGIN
                ALTER TABLE [Products] ADD [CompatibleVehicleModels] nvarchar(max) NULL;
            END

            -- ApplicationUser patches
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Certifications')
            BEGIN
                ALTER TABLE [AspNetUsers] ADD [Certifications] nvarchar(max) NULL;
                ALTER TABLE [AspNetUsers] ADD [Specializations] nvarchar(max) NULL;
                ALTER TABLE [AspNetUsers] ADD [IsVerifiedExpert] bit NOT NULL DEFAULT 0;
            END

            -- Model consistency patches
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'PaymentMethod')
            BEGIN
                ALTER TABLE [Payments] ADD [PaymentMethod] nvarchar(max) NOT NULL DEFAULT 'Online';
            END
            ELSE IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
            BEGIN
                -- Ensure no nulls exist if column was created as nullable previously
                EXEC('UPDATE [Payments] SET [PaymentMethod] = ''Online'' WHERE [PaymentMethod] IS NULL');
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Ratings') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Ratings') AND name = 'Comment')
            BEGIN
                ALTER TABLE [Ratings] ADD [Comment] nvarchar(max) NULL;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Commissions') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Commissions') AND name = 'Role')
            BEGIN
                ALTER TABLE [Commissions] ADD [Role] nvarchar(max) NOT NULL DEFAULT 'Provider';
            END
            ELSE IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Commissions')
            BEGIN
                EXEC('UPDATE [Commissions] SET [Role] = ''Provider'' WHERE [Role] IS NULL');
            END

            -- Order return management patches
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'IsReturnApproved')
            BEGIN
                ALTER TABLE [Orders] ADD [IsReturnApproved] bit NULL;
                ALTER TABLE [Orders] ADD [ReturnReason] nvarchar(max) NULL;
                ALTER TABLE [Orders] ADD [ReturnRequestedAt] datetime2 NULL;
            END

            -- ActivityLog missing column patch
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActivityLogs') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ActivityLogs') AND name = 'PerformedBy')
            BEGIN
                ALTER TABLE [ActivityLogs] ADD [PerformedBy] nvarchar(max) NOT NULL DEFAULT 'System';
            END
            ELSE IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ActivityLogs')
            BEGIN
                EXEC('UPDATE [ActivityLogs] SET [PerformedBy] = ''System'' WHERE [PerformedBy] IS NULL');
            END
            -- ServiceRequest patches
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceRequests') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'BreakdownType')
            BEGIN
                ALTER TABLE [ServiceRequests] ADD [BreakdownType] int NULL;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceRequests') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'RequestType')
            BEGIN
                ALTER TABLE [ServiceRequests] ADD [RequestType] int NOT NULL DEFAULT 0;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceRequests') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'Latitude')
            BEGIN
                ALTER TABLE [ServiceRequests] ADD [Latitude] float NULL;
                ALTER TABLE [ServiceRequests] ADD [Longitude] float NULL;
                ALTER TABLE [ServiceRequests] ADD [LocationAddress] nvarchar(500) NULL;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceRequests') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'BatteryStateOfHealth')
            BEGIN
                ALTER TABLE [ServiceRequests] ADD [BatteryStateOfHealth] int NULL;
                ALTER TABLE [ServiceRequests] ADD [KilometersDriven] bigint NULL;
                ALTER TABLE [ServiceRequests] ADD [VehicleModelName] nvarchar(100) NULL;
            END

            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceRequests') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ServiceRequests') AND name = 'AcceptedAt')
            BEGIN
                ALTER TABLE [ServiceRequests] ADD [AcceptedAt] datetime2 NULL;
                ALTER TABLE [ServiceRequests] ADD [CompletedAt] datetime2 NULL;
                ALTER TABLE [ServiceRequests] ADD [UpdatedAt] datetime2 NULL;
                ALTER TABLE [ServiceRequests] ADD [Notes] nvarchar(1000) NULL;
            END
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Ratings') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Ratings') AND name = 'ServiceRequestId')
            BEGIN
                ALTER TABLE [Ratings] ADD [ServiceRequestId] int NULL;
            END
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Commissions') AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Commissions') AND name = 'PaidAt')
            BEGIN
                ALTER TABLE [Commissions] ADD [PaidAt] datetime2 NULL;
            END
        ";
        await context.Database.ExecuteSqlRawAsync(patchSql);

        await DbSeeder.SeedRolesAndAdmin(scope.ServiceProvider);
        
        // Wait a small bit or ensure consistency
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@serviceplatform.com");
        if (admin != null) {
            await DbSeeder.SeedDemoData(scope.ServiceProvider);
        }
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EV Service Platform API v1");
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCors("DefaultPolicy");
    app.UseRateLimiter();

    app.UseResponseCaching();
    app.UseSession();

    // Custom middleware
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    // SignalR hub
    app.MapHub<NotificationHub>("/hubs/notification");
    app.MapHub<ServicePlatform.Hubs.ChatHub>("/hubs/chat");

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
