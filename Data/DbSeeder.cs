using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServicePlatform.Models;

namespace ServicePlatform.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdmin(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "User", "ServiceProvider", "Shopkeeper" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin user
        var adminEmail = "admin@serviceplatform.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Global Operations Admin",
                Mobile = "9999999999",
                State = "Maharashtra",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // MNC Service Providers
        var providers = new[]
        {
            new { Email = "ather@service.com", Name = "Ather Energy Service", Role = "ServiceProvider" },
            new { Email = "ola@service.com", Name = "Ola Electric Care", Role = "ServiceProvider" },
            new { Email = "tesla@service.com", Name = "Tesla Global Service", Role = "ServiceProvider" },
            new { Email = "tata@service.com", Name = "Tata Motors EV Division", Role = "ServiceProvider" }
        };

        foreach (var p in providers)
        {
            var user = await userManager.FindByEmailAsync(p.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = p.Email,
                    Email = p.Email,
                    FullName = p.Name,
                    Mobile = "9876543210",
                    State = "Karnataka",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(user, "Provider@123456");
                if (result.Succeeded) await userManager.AddToRoleAsync(user, p.Role);
            }
        }

        // Demo Shopkeeper
        var shopEmail = "shop@serviceplatform.com";
        var shopUser = await userManager.FindByEmailAsync(shopEmail);
        if (shopUser == null)
        {
            shopUser = new ApplicationUser
            {
                UserName = shopEmail,
                Email = shopEmail,
                FullName = "EV Accessories Hub",
                Mobile = "7777777777",
                State = "Delhi",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(shopUser, "Shop@123456");
            if (result.Succeeded) await userManager.AddToRoleAsync(shopUser, "Shopkeeper");
        }

        // Demo User
        var userEmail = "user@serviceplatform.com";
        var demoUser = await userManager.FindByEmailAsync(userEmail);
        if (demoUser == null)
        {
            demoUser = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                FullName = "Rohan Sharma",
                Mobile = "6666666666",
                State = "Maharashtra",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(demoUser, "User@123456");
            if (result.Succeeded) await userManager.AddToRoleAsync(demoUser, "User");
        }
    }

    public static async Task SeedDemoData(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var shopkeeper = await userManager.FindByEmailAsync("shop@serviceplatform.com");
        var atherProvider = await userManager.FindByEmailAsync("ather@service.com");
        var olaProvider = await userManager.FindByEmailAsync("ola@service.com");
        var demoUser = await userManager.FindByEmailAsync("user@serviceplatform.com");

        if (shopkeeper == null || atherProvider == null || olaProvider == null || demoUser == null) return;

        // Clean existing data for a fresh professional seed
        if (await context.Products.AnyAsync())
        {
            context.OrderItems.RemoveRange(context.OrderItems);
            context.Orders.RemoveRange(context.Orders);
            context.CartItems.RemoveRange(context.CartItems);
            context.Payments.RemoveRange(context.Payments);
            context.Products.RemoveRange(context.Products);
            await context.SaveChangesAsync();
        }

        // 1. Seed 20 Products (EV Focused)
        var productCategories = new[] { "Chargers", "Performance", "Safety", "Appearance" };
        var products = new List<Product>();
        for (int i = 1; i <= 20; i++)
        {
            products.Add(new Product
            {
                Name = $"EV Product Elite {i}",
                Description = $"High-performance EV accessory {i} designed for modern electric vehicles. Built to international standards.",
                Price = 1000 + (i * 500),
                DiscountPrice = 900 + (i * 450),
                Category = productCategories[i % 4],
                ImageUrl = $"/images/products/product_{i % 8 + 1}.png",
                IsActive = true,
                StockQuantity = 10 + i,
                SKU = $"EV-PROD-{100 + i}",
                ShopkeeperId = shopkeeper.Id
            });
        }
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // 2. Seed 20 Vehicle Models
        if (!await context.VehicleModels.AnyAsync())
        {
            var vehicleModels = new List<VehicleModel>();
            var manufacturers = new[] { 
                new { Provider = atherProvider!, Name = "Ather 450X", Type = VehicleType.Scooter },
                new { Provider = atherProvider!, Name = "Ather 450S", Type = VehicleType.Scooter },
                new { Provider = olaProvider!, Name = "Ola S1 Pro", Type = VehicleType.Scooter },
                new { Provider = olaProvider!, Name = "Ola S1 Air", Type = VehicleType.Scooter }
            };

            for (int i = 0; i < 20; i++)
            {
                var m = manufacturers[i % manufacturers.Length];
                vehicleModels.Add(new VehicleModel
                {
                    Name = $"{m.Name} Gen {i/4 + 1}",
                    Type = m.Type,
                    ModelNumber = $"MOD-{1000 + i}",
                    ReleaseYear = 2020 + (i % 5),
                    Description = $"The ultimate electric {m.Type} experience with advanced telematics.",
                    ServiceProviderId = m.Provider.Id,
                    IsActive = true
                });
            }
            context.VehicleModels.AddRange(vehicleModels);
            await context.SaveChangesAsync();
        }

        // 3. Seed 20 Service Requests
        if (!await context.ServiceRequests.AnyAsync())
        {
            var models = await context.VehicleModels.Take(5).ToListAsync();
            var serviceRequests = new List<ServiceRequest>();
            for (int i = 1; i <= 20; i++)
            {
                serviceRequests.Add(new ServiceRequest
                {
                    Title = $"EV Service Maintenance {i}",
                    Description = $"Routine checkup and diagnostic for EV component {i}. Issues with range detected.",
                    RequestType = i % 3 == 0 ? ServiceRequestType.Breakdown : ServiceRequestType.General,
                    Status = (ServiceRequestStatus)(i % 6),
                    Category = i % 2 == 0 ? "Battery" : "Motor",
                    LocationAddress = $"{i * 10}, Business Park, Mumbai, 400001",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UserId = demoUser.Id,
                    ServiceProviderId = i % 2 == 0 ? atherProvider.Id : olaProvider.Id,
                    VehicleModelId = models[i % models.Count].Id,
                    BatteryStateOfHealth = 95 - i,
                    KilometersDriven = i * 1500,
                    VehicleModelName = models[i % models.Count].Name
                });
            }
            context.ServiceRequests.AddRange(serviceRequests);
            await context.SaveChangesAsync();
        }

        // 4. Seed 20 Orders and matching 20 Payments
        if (!await context.Orders.AnyAsync())
        {
            var dbProducts = await context.Products.ToListAsync();
            for (int i = 1; i <= 20; i++)
            {
                var order = new Order
                {
                    OrderNumber = $"ORD-MNC-{1000 + i}",
                    TotalAmount = 5000 + (i * 200),
                    TaxAmount = (5000 + (i * 200)) * 0.18m,
                    Status = (OrderStatus)(i % 5),
                    ShippingAddress = $"Customer Residence {i}, High Rise, Mumbai",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UserId = demoUser.Id
                };
                context.Orders.Add(order);
                await context.SaveChangesAsync(); // Get ID

                context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = dbProducts[i % dbProducts.Count].Id,
                    Quantity = 1,
                    UnitPrice = order.TotalAmount,
                    TotalPrice = order.TotalAmount
                });

                context.Payments.Add(new Payment
                {
                    Amount = order.TotalAmount,
                    Currency = "INR",
                    Status = i % 5 == 4 ? PaymentStatus.Failed : PaymentStatus.Completed,
                    Gateway = PaymentGateway.Razorpay,
                    TransactionId = $"TXN-EV-{5000 + i}",
                    CreatedAt = order.CreatedAt,
                    CompletedAt = order.CreatedAt.AddMinutes(5),
                    UserId = demoUser.Id,
                    OrderId = order.Id
                });
            }
            await context.SaveChangesAsync();
        }

        // 5. Seed 20 Ratings
        if (!await context.Ratings.AnyAsync())
        {
            var requests = await context.ServiceRequests.Where(r => r.Status == ServiceRequestStatus.Completed).ToListAsync();
            for (int i = 0; i < Math.Min(20, requests.Count); i++)
            {
                context.Ratings.Add(new Rating
                {
                    Stars = (i % 5) + 1,
                    ReviewText = $"Excellent service maintenance! The EV tech was very professional. (Demo {i})",
                    UserId = demoUser.Id,
                    ServiceProviderId = requests[i].ServiceProviderId!,
                    ServiceRequestId = requests[i].Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            await context.SaveChangesAsync();
        }

        // 6. Seed 20 Feedbacks
        if (!await context.Feedbacks.AnyAsync())
        {
            for (int i = 1; i <= 20; i++)
            {
                context.Feedbacks.Add(new Feedback
                {
                    Subject = $"System Feedback {i}",
                    Message = $"This is an automated production-level feedback entry {i} for testing the admin resolve system. The UI is great!",
                    IsResolved = i % 3 == 0,
                    AdminResponse = i % 3 == 0 ? "Thank you for your valuable feedback! We have noted your points." : null,
                    UserId = demoUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            await context.SaveChangesAsync();
        }

        // 7. Seed 20 Charging Stations
        if (!await context.ChargingStations.AnyAsync())
        {
            var cities = new[] { "Mumbai", "Delhi", "Bangalore", "Pune", "Hyderabad" };
            for (int i = 1; i <= 20; i++)
            {
                context.ChargingStations.Add(new ChargingStation
                {
                    Name = $"EcoCharge Hub {i}",
                    Address = $"{i * 5}, Green Tech Park, {cities[i % 5]}",
                    City = cities[i % 5],
                    State = "State " + (i % 3 + 1),
                    Latitude = 19.0760 + (i * 0.01),
                    Longitude = 72.8777 + (i * 0.01),
                    ConnectorTypes = "Type 2, CCS2",
                    PowerOutput = i % 2 == 0 ? "50kW" : "22kW",
                    IsActive = true,
                    CurrentStatus = "Available",
                    QueueCount = 0,
                    LastStatusUpdate = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();
        }
    }
}
