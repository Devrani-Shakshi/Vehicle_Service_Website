namespace ServicePlatform.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalServiceProviders { get; set; }
    public int TotalShopkeepers { get; set; }
    public int TotalOrders { get; set; }
    public int TotalServiceRequests { get; set; }
    public int PendingRequests { get; set; }
    public int CompletedRequests { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }

    // Chart data
    public List<MonthlyRevenueData> RevenueData { get; set; } = new();
    public List<MonthlyActivityData> ServiceActivityData { get; set; } = new();
    public List<MonthlyUserGrowth> UserGrowthData { get; set; } = new();

    // Recent activity
    public List<RecentServiceRequestDto> RecentRequests { get; set; } = new();
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
    public List<RecentPaymentDto> RecentPayments { get; set; } = new();
}

public class MonthlyRevenueData
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class MonthlyActivityData
{
    public string Month { get; set; } = string.Empty;
    public int Requests { get; set; }
    public int Completed { get; set; }
}

public class MonthlyUserGrowth
{
    public string Month { get; set; } = string.Empty;
    public int Users { get; set; }
}

public class RecentServiceRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentPaymentDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Gateway { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
