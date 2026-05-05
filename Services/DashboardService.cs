using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServicePlatform.Data;
using ServicePlatform.Models;
using ServicePlatform.Services.Interfaces;
using ServicePlatform.ViewModels.Admin;
using ServicePlatform.ViewModels.Service;
using ServicePlatform.ViewModels.Shop;

namespace ServicePlatform.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IServiceRequestService _requestService;

    public DashboardService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, IServiceRequestService requestService)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
        _requestService = requestService;
    }

    private decimal GetCommissionRate()
    {
        var ratePercent = _configuration.GetValue<decimal>("GlobalSettings:CommissionRate", 5);
        return ratePercent / 100m; // Convert 5 → 0.05
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var now = DateTime.UtcNow;
        await RecalculateCommissionsAsync();

        var commissions = await _context.Commissions.Include(c => c.User).ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalUsers = (await _userManager.GetUsersInRoleAsync("User")).Count,
            TotalServiceProviders = (await _userManager.GetUsersInRoleAsync("ServiceProvider")).Count,
            TotalShopkeepers = (await _userManager.GetUsersInRoleAsync("Shopkeeper")).Count,
            TotalPlatformRevenue = commissions.Where(c => c.IsPaid).Sum(c => c.CommissionAmount),
            MonthlyPlatformRevenue = commissions.Where(c => c.IsPaid && c.PaidAt != null && c.PaidAt.Value.Month == now.Month && c.PaidAt.Value.Year == now.Year).Sum(c => c.CommissionAmount),
            PendingCommissionAmount = commissions.Where(c => !c.IsPaid).Sum(c => c.CommissionAmount)
        };

        // Revenue chart data (last 6 months based on Commissions)
        model.RevenueData = Enumerable.Range(0, 6).Select(i =>
        {
            var month = now.AddMonths(-5 + i);
            return new MonthlyRevenueData
            {
                Month = month.ToString("MMM"),
                Amount = commissions
                    .Where(c => c.Month == month.Month && c.Year == month.Year)
                    .Sum(c => c.CommissionAmount)
            };
        }).ToList();

        // User growth data
        model.UserGrowthData = Enumerable.Range(0, 6).Select(i =>
        {
            var month = now.AddMonths(-5 + i);
            return new MonthlyUserGrowth
            {
                Month = month.ToString("MMM"),
                Users = _context.Users.Count(u => u.CreatedAt.Month == month.Month && u.CreatedAt.Year == month.Year)
            };
        }).ToList();

        // Recent Commissions
        model.RecentCommissions = commissions
            .OrderByDescending(c => c.Year).ThenByDescending(c => c.Month).ThenByDescending(c => c.Id)
            .Take(5)
            .Select(c => new RecentCommissionDto
            {
                Id = c.Id,
                VendorName = c.User?.FullName ?? "Unknown",
                MonthYear = $"{new DateTime(c.Year, c.Month, 1):MMM yyyy}",
                Amount = c.CommissionAmount,
                Status = c.IsPaid ? "Paid" : "Pending"
            }).ToList();

        return model;
    }

    public async Task<UserDashboardViewModel> GetUserDashboardAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return new UserDashboardViewModel
        {
            UserName = user?.FullName ?? "User",
            PendingRequests = await _context.ServiceRequests.CountAsync(r => r.UserId == userId && r.Status == ServiceRequestStatus.Pending),
            CompletedRequests = await _context.ServiceRequests.CountAsync(r => r.UserId == userId && r.Status == ServiceRequestStatus.Completed),
            TotalOrders = await _context.Orders.CountAsync(o => o.UserId == userId),
            UpcomingAppointments = await _context.Appointments.CountAsync(a => a.UserId == userId && a.ScheduledDate > DateTime.UtcNow && a.Status != AppointmentStatus.Cancelled),
            RecentRequests = await _context.ServiceRequests
                .Include(r => r.ServiceProvider)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync(),
            UpcomingAppointmentsList = await _context.Appointments
                .Include(a => a.ServiceProvider)
                .Where(a => a.UserId == userId && a.ScheduledDate > DateTime.UtcNow)
                .OrderBy(a => a.ScheduledDate)
                .Take(5)
                .ToListAsync(),
            RecentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync(),
            BatteryHealthHistory = await _context.ServiceRequests
                .Where(r => r.UserId == userId && r.BatteryStateOfHealth != null)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new BatteryHealthPoint { Date = r.CreatedAt.ToString("MMM dd"), Health = r.BatteryStateOfHealth ?? 0 })
                .ToListAsync(),
            VehicleHealthScore = await _requestService.GetVehicleHealthScoreAsync(userId)
        };
    }

    public async Task<ServiceProviderDashboardViewModel> GetProviderDashboardAsync(string providerId)
    {
        var user = await _userManager.FindByIdAsync(providerId);
        var ratings = await _context.Ratings.Where(r => r.ServiceProviderId == providerId).ToListAsync();

        return new ServiceProviderDashboardViewModel
        {
            ProviderName = user?.FullName ?? "Provider",
            PendingRequests = await _context.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.Pending),
            AcceptedRequests = await _context.ServiceRequests.CountAsync(r => r.ServiceProviderId == providerId && r.Status == ServiceRequestStatus.Accepted),
            CompletedRequests = await _context.ServiceRequests.CountAsync(r => r.ServiceProviderId == providerId && r.Status == ServiceRequestStatus.Completed),
            TotalEarnings = (int)(await _context.Payments
                .Where(p => p.ServiceRequest != null && p.ServiceRequest.ServiceProviderId == providerId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => (decimal?)p.Amount) ?? 0),
            AverageRating = ratings.Any() ? ratings.Average(r => r.Stars) : 0,
            TotalReviews = ratings.Count,
            PendingRequestsList = await _context.ServiceRequests
                .Include(r => r.User)
                .Where(r => r.Status == ServiceRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync(),
            ActiveRequests = await _context.ServiceRequests
                .Include(r => r.User)
                .Where(r => r.ServiceProviderId == providerId && (r.Status == ServiceRequestStatus.Accepted || r.Status == ServiceRequestStatus.InProgress))
                .OrderByDescending(r => r.AcceptedAt)
                .ToListAsync(),
            RecentRatings = await _context.Ratings
                .Include(r => r.User)
                .Where(r => r.ServiceProviderId == providerId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync(),
            CategoryStats = await _context.ServiceRequests
                .Where(r => r.ServiceProviderId == providerId && r.Category != null)
                .GroupBy(r => r.Category!)
                .Select(g => new ServiceCategoryStat { Category = g.Key, Count = g.Count() })
                .ToListAsync(),
            RevenueTrend = Enumerable.Range(0, 6).Select(i => {
                var date = DateTime.UtcNow.AddMonths(-5 + i);
                return new MonthlyRevenuePoint {
                    Month = date.ToString("MMM"),
                    Amount = _context.Payments
                        .Where(p => p.ServiceRequest != null && p.ServiceRequest.ServiceProviderId == providerId && 
                               p.Status == PaymentStatus.Completed && 
                               p.CompletedAt != null && p.CompletedAt.Value.Month == date.Month && p.CompletedAt.Value.Year == date.Year)
                        .Sum(p => (decimal?)p.Amount) ?? 0
                };
            }).ToList()
        };
    }

    public async Task<ShopkeeperDashboardViewModel> GetShopkeeperDashboardAsync(string shopkeeperId)
    {
        var user = await _userManager.FindByIdAsync(shopkeeperId);
        var now = DateTime.UtcNow;

        var model = new ShopkeeperDashboardViewModel
        {
            ShopkeeperName = user?.FullName ?? "Shopkeeper",
            TotalProducts = await _context.Products.CountAsync(p => p.ShopkeeperId == shopkeeperId),
            ActiveProducts = await _context.Products.CountAsync(p => p.ShopkeeperId == shopkeeperId && p.IsActive),
            TotalOrders = await _context.Orders.CountAsync(o => o.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId)),
            PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending && o.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId)),
            TotalRevenue = await _context.OrderItems
                .Where(oi => oi.Product.ShopkeeperId == shopkeeperId && oi.Order.Status != OrderStatus.Cancelled)
                .SumAsync(oi => (decimal?)oi.TotalPrice) ?? 0,
            MonthlyRevenue = await _context.OrderItems
                .Where(oi => oi.Product.ShopkeeperId == shopkeeperId &&
                       oi.Order.CreatedAt.Month == now.Month && oi.Order.CreatedAt.Year == now.Year &&
                       oi.Order.Status != OrderStatus.Cancelled)
                .SumAsync(oi => (decimal?)oi.TotalPrice) ?? 0,
            RecentProducts = await _context.Products
                .Where(p => p.ShopkeeperId == shopkeeperId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync(),
            RecentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId))
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync(),
            RecentPayments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Order)
                .Where(p => p.Order != null && p.Order.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId))
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        // Revenue Trend (Last 6 Months)
        for (int i = 5; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var amount = await _context.OrderItems
                .Where(oi => oi.Product.ShopkeeperId == shopkeeperId &&
                       oi.Order.CreatedAt.Month == date.Month && 
                       oi.Order.CreatedAt.Year == date.Year &&
                       oi.Order.Status != OrderStatus.Cancelled)
                .SumAsync(oi => (decimal?)oi.TotalPrice) ?? 0;
            
            model.RevenueTrend.Add(new RevenueStat { Month = date.ToString("MMM"), Amount = amount });
        }

        // Category Distribution
        model.CategoryDistribution = await _context.Products
            .Where(p => p.ShopkeeperId == shopkeeperId)
            .GroupBy(p => p.Category)
            .Select(g => new CategoryStat { Category = g.Key ?? "Uncategorized", Count = g.Count() })
            .ToListAsync();

        return model;
    }

    // Commission Implementations
    public async Task<IEnumerable<Commission>> GetCommissionsAsync()
    {
        await RecalculateCommissionsAsync(); // Ensure we have latest data
        return await _context.Commissions
            .Include(c => c.User)
            .OrderByDescending(c => c.Year).ThenByDescending(c => c.Month)
            .ToListAsync();
    }

    public async Task MarkCommissionPaidAsync(int id)
    {
        var commission = await _context.Commissions.FindAsync(id);
        if (commission != null)
        {
            commission.IsPaid = true;
            commission.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RecalculateCommissionsAsync()
    {
        var now = DateTime.UtcNow;
        var lastMonth = now.AddMonths(-1);
        var month = lastMonth.Month;
        var year = lastMonth.Year;
        var rate = GetCommissionRate();

        // 1. Calculate for Service Providers
        var providers = await _userManager.GetUsersInRoleAsync("ServiceProvider");
        foreach (var provider in providers)
        {
            var existing = await _context.Commissions.AnyAsync(c => c.UserId == provider.Id && c.Month == month && c.Year == year);
            if (existing) continue;

            var income = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.ServiceRequest != null && 
                       p.ServiceRequest.ServiceProviderId == provider.Id &&
                       p.CompletedAt != null && p.CompletedAt.Value.Month == month && p.CompletedAt.Value.Year == year)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            if (income > 0)
            {
                _context.Commissions.Add(new Commission 
                { 
                    UserId = provider.Id, 
                    Month = month, 
                    Year = year, 
                    TotalIncome = income, 
                    CommissionAmount = income * rate, 
                    AppliedPercentage = rate * 100,
                    IsPaid = false 
                });
            }
        }

        // 2. Calculate for Shopkeepers
        var shopkeepers = await _userManager.GetUsersInRoleAsync("Shopkeeper");
        foreach (var shopkeeper in shopkeepers)
        {
            var existing = await _context.Commissions.AnyAsync(c => c.UserId == shopkeeper.Id && c.Month == month && c.Year == year);
            if (existing) continue;

            var income = await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o!.OrderItems).ThenInclude(oi => oi.Product)
                .Where(p => p.Status == PaymentStatus.Completed && p.Order != null && 
                       p.Order.OrderItems.Any(oi => oi.Product != null && oi.Product.ShopkeeperId == shopkeeper.Id) &&
                       p.CompletedAt != null && p.CompletedAt.Value.Month == month && p.CompletedAt.Value.Year == year)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            if (income > 0)
            {
                _context.Commissions.Add(new Commission 
                { 
                    UserId = shopkeeper.Id, 
                    Month = month, 
                    Year = year, 
                    TotalIncome = income, 
                    CommissionAmount = income * rate, 
                    AppliedPercentage = rate * 100,
                    IsPaid = false 
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
