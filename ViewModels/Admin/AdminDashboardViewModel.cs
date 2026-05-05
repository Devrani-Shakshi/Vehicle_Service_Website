namespace ServicePlatform.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalServiceProviders { get; set; }
    public int TotalShopkeepers { get; set; }
    public decimal TotalPlatformRevenue { get; set; }
    public decimal MonthlyPlatformRevenue { get; set; }
    public decimal PendingCommissionAmount { get; set; }

    // Chart data
    public List<MonthlyRevenueData> RevenueData { get; set; } = new();
    public List<MonthlyUserGrowth> UserGrowthData { get; set; } = new();

    // Recent activity strictly administrative
    public List<RecentCommissionDto> RecentCommissions { get; set; } = new();
}

public class MonthlyRevenueData
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class MonthlyUserGrowth
{
    public string Month { get; set; } = string.Empty;
    public int Users { get; set; }
}

public class RecentCommissionDto
{
    public int Id { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string MonthYear { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
