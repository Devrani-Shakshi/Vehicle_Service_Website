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

    public DashboardService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var sixMonthsAgo = now.AddMonths(-6);

        var model = new AdminDashboardViewModel
        {
            TotalUsers = (await _userManager.GetUsersInRoleAsync("User")).Count,
            TotalServiceProviders = (await _userManager.GetUsersInRoleAsync("ServiceProvider")).Count,
            TotalShopkeepers = (await _userManager.GetUsersInRoleAsync("Shopkeeper")).Count,
            TotalOrders = await _context.Orders.CountAsync(),
            TotalServiceRequests = await _context.ServiceRequests.CountAsync(),
            PendingRequests = await _context.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.Pending),
            CompletedRequests = await _context.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.Completed),
            TotalRevenue = await _context.Payments.Where(p => p.Status == PaymentStatus.Completed).SumAsync(p => p.Amount),
            MonthlyRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.CompletedAt != null && p.CompletedAt.Value.Month == now.Month && p.CompletedAt.Value.Year == now.Year)
                .SumAsync(p => p.Amount)
        };

        // Revenue chart data (last 6 months)
        model.RevenueData = Enumerable.Range(0, 6).Select(i =>
        {
            var month = now.AddMonths(-5 + i);
            return new MonthlyRevenueData
            {
                Month = month.ToString("MMM"),
                Amount = _context.Payments
                    .Where(p => p.Status == PaymentStatus.Completed && p.CompletedAt != null &&
                           p.CompletedAt.Value.Month == month.Month && p.CompletedAt.Value.Year == month.Year)
                    .Sum(p => p.Amount)
            };
        }).ToList();

        // Service activity data
        model.ServiceActivityData = Enumerable.Range(0, 6).Select(i =>
        {
            var month = now.AddMonths(-5 + i);
            return new MonthlyActivityData
            {
                Month = month.ToString("MMM"),
                Requests = _context.ServiceRequests
                    .Count(r => r.CreatedAt.Month == month.Month && r.CreatedAt.Year == month.Year),
                Completed = _context.ServiceRequests
                    .Count(r => r.Status == ServiceRequestStatus.Completed && r.CompletedAt != null &&
                           r.CompletedAt.Value.Month == month.Month && r.CompletedAt.Value.Year == month.Year)
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

        // Recent activity
        model.RecentRequests = await _context.ServiceRequests
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new RecentServiceRequestDto
            {
                Id = r.Id,
                Title = r.Title,
                UserName = r.User.FullName,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            }).ToListAsync();

        model.RecentOrders = await _context.Orders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.User.FullName,
                Amount = o.TotalAmount,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            }).ToListAsync();

        model.RecentPayments = await _context.Payments
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPaymentDto
            {
                Id = p.Id,
                UserName = p.User.FullName,
                Amount = p.Amount,
                Gateway = p.Gateway.ToString(),
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt
            }).ToListAsync();

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
                .ToListAsync()
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
            TotalEarnings = (int)await _context.Payments
                .Where(p => p.ServiceRequest != null && p.ServiceRequest.ServiceProviderId == providerId && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount),
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
                .ToListAsync()
        };
    }

    public async Task<ShopkeeperDashboardViewModel> GetShopkeeperDashboardAsync(string shopkeeperId)
    {
        var user = await _userManager.FindByIdAsync(shopkeeperId);
        var now = DateTime.UtcNow;

        return new ShopkeeperDashboardViewModel
        {
            ShopkeeperName = user?.FullName ?? "Shopkeeper",
            TotalProducts = await _context.Products.CountAsync(p => p.ShopkeeperId == shopkeeperId),
            ActiveProducts = await _context.Products.CountAsync(p => p.ShopkeeperId == shopkeeperId && p.IsActive),
            TotalOrders = await _context.Orders.CountAsync(o => o.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId)),
            PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending && o.OrderItems.Any(oi => oi.Product.ShopkeeperId == shopkeeperId)),
            TotalRevenue = await _context.OrderItems
                .Where(oi => oi.Product.ShopkeeperId == shopkeeperId && oi.Order.Status != OrderStatus.Cancelled)
                .SumAsync(oi => oi.TotalPrice),
            MonthlyRevenue = await _context.OrderItems
                .Where(oi => oi.Product.ShopkeeperId == shopkeeperId &&
                       oi.Order.CreatedAt.Month == now.Month && oi.Order.CreatedAt.Year == now.Year &&
                       oi.Order.Status != OrderStatus.Cancelled)
                .SumAsync(oi => oi.TotalPrice),
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
                .ToListAsync()
        };
    }
}
