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
                FullName = "System Administrator",
                Mobile = "9999999999",
                State = "Maharashtra",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed demo Service Provider
        var providerEmail = "provider@serviceplatform.com";
        var providerUser = await userManager.FindByEmailAsync(providerEmail);
        if (providerUser == null)
        {
            providerUser = new ApplicationUser
            {
                UserName = providerEmail,
                Email = providerEmail,
                FullName = "Demo Service Provider",
                Mobile = "8888888888",
                State = "Maharashtra",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(providerUser, "Provider@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(providerUser, "ServiceProvider");
            }
        }

        // Seed demo Shopkeeper
        var shopEmail = "shop@serviceplatform.com";
        var shopUser = await userManager.FindByEmailAsync(shopEmail);
        if (shopUser == null)
        {
            shopUser = new ApplicationUser
            {
                UserName = shopEmail,
                Email = shopEmail,
                FullName = "Demo Shopkeeper",
                Mobile = "7777777777",
                State = "Maharashtra",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(shopUser, "Shop@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(shopUser, "Shopkeeper");
            }
        }

        // Seed demo User
        var userEmail = "user@serviceplatform.com";
        var demoUser = await userManager.FindByEmailAsync(userEmail);
        if (demoUser == null)
        {
            demoUser = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                FullName = "Demo User",
                Mobile = "6666666666",
                State = "Maharashtra",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(demoUser, "User@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(demoUser, "User");
            }
        }
    }

    public static async Task SeedDemoData(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Get demo users
        var shopkeeper = await userManager.FindByEmailAsync("shop@serviceplatform.com");
        var provider = await userManager.FindByEmailAsync("provider@serviceplatform.com");
        var demoUser = await userManager.FindByEmailAsync("user@serviceplatform.com");
        var admin = await userManager.FindByEmailAsync("admin@serviceplatform.com");

        if (shopkeeper == null || provider == null || demoUser == null || admin == null)
            return;

        // ── Products ──────────────────────────────────────────────
        if (!await context.Products.AnyAsync())
        {
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Wireless Bluetooth Speaker",
                    Description = "Portable 20W speaker with deep bass, IPX7 waterproof rating, and 12-hour battery life. Perfect for outdoor adventures.",
                    Price = 2499.00m,
                    DiscountPrice = 1999.00m,
                    Category = "Electronics",
                    ImageUrl = "/images/products/speaker.png",
                    StockQuantity = 50,
                    SKU = "ELEC-SPK-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Professional Tool Kit (120 Pcs)",
                    Description = "Complete home repair toolkit with screwdrivers, pliers, wrenches, hammer, tape measure, and carrying case.",
                    Price = 3499.00m,
                    DiscountPrice = 2799.00m,
                    Category = "Tools",
                    ImageUrl = "/images/products/toolkit.png",
                    StockQuantity = 30,
                    SKU = "TOOL-KIT-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Smart LED Desk Lamp",
                    Description = "Touch-controlled LED desk lamp with 5 color temperatures, USB charging port, and adjustable brightness.",
                    Price = 1899.00m,
                    Category = "Home Appliances",
                    ImageUrl = "/images/products/lamp.png",
                    StockQuantity = 75,
                    SKU = "HOME-LMP-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Stainless Steel Water Purifier",
                    Description = "7-stage RO+UV water purifier with 10L storage tank and mineral cartridge for clean drinking water.",
                    Price = 12999.00m,
                    DiscountPrice = 10499.00m,
                    Category = "Home Appliances",
                    ImageUrl = "/images/products/purifier.png",
                    StockQuantity = 15,
                    SKU = "HOME-PUR-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Cordless Drill Machine",
                    Description = "18V lithium-ion cordless drill with 2-speed settings, 13mm chuck, LED worklight, and two batteries.",
                    Price = 4599.00m,
                    DiscountPrice = 3999.00m,
                    Category = "Tools",
                    ImageUrl = "/images/products/drill.png",
                    StockQuantity = 25,
                    SKU = "TOOL-DRL-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Automatic Voltage Stabilizer",
                    Description = "5KVA voltage stabilizer for AC, refrigerator, and other heavy appliances. Wide input range 90V-300V.",
                    Price = 3299.00m,
                    Category = "Electronics",
                    ImageUrl = "/images/products/stabilizer.png",
                    StockQuantity = 20,
                    SKU = "ELEC-STB-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Eco-Friendly Cleaning Kit",
                    Description = "All-purpose organic cleaning set with floor cleaner, glass cleaner, disinfectant spray, and microfiber cloths.",
                    Price = 899.00m,
                    DiscountPrice = 699.00m,
                    Category = "Cleaning",
                    ImageUrl = "/images/products/cleaningkit.png",
                    StockQuantity = 100,
                    SKU = "CLN-KIT-001",
                    ShopkeeperId = shopkeeper.Id
                },
                new Product
                {
                    Name = "Digital Multimeter",
                    Description = "Professional-grade auto-ranging digital multimeter with backlit display. Measures voltage, current, resistance, and capacitance.",
                    Price = 1599.00m,
                    DiscountPrice = 1299.00m,
                    Category = "Tools",
                    ImageUrl = "/images/products/multimeter.png",
                    StockQuantity = 40,
                    SKU = "TOOL-MTR-001",
                    ShopkeeperId = shopkeeper.Id
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // ── Service Requests ──────────────────────────────────────
        if (!await context.ServiceRequests.AnyAsync())
        {
            var serviceRequests = new List<ServiceRequest>
            {
                new ServiceRequest
                {
                    Title = "Kitchen Sink Leaking",
                    Description = "The kitchen sink pipe has a persistent leak near the joint. Water is dripping continuously and has caused water damage to the cabinet below.",
                    RequestType = ServiceRequestType.Urgent,
                    Status = ServiceRequestStatus.Completed,
                    Category = "Plumbing",
                    Latitude = 19.0760,
                    Longitude = 72.8777,
                    LocationAddress = "202, Andheri West, Mumbai, Maharashtra 400053",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    AcceptedAt = DateTime.UtcNow.AddDays(-14),
                    CompletedAt = DateTime.UtcNow.AddDays(-13),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new ServiceRequest
                {
                    Title = "Electrical Wiring Repair",
                    Description = "Multiple power outlets in the living room have stopped working. Suspected wiring fault behind the wall. Need complete inspection and repair.",
                    RequestType = ServiceRequestType.General,
                    Status = ServiceRequestStatus.Completed,
                    Category = "Electrical",
                    Latitude = 19.0825,
                    Longitude = 72.8906,
                    LocationAddress = "45, Bandra East, Mumbai, Maharashtra 400051",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    AcceptedAt = DateTime.UtcNow.AddDays(-9),
                    CompletedAt = DateTime.UtcNow.AddDays(-8),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new ServiceRequest
                {
                    Title = "Deep Home Cleaning",
                    Description = "Need a thorough deep cleaning for a 3BHK apartment including kitchen, bathrooms, balconies, and all rooms. Moving into new apartment.",
                    RequestType = ServiceRequestType.General,
                    Status = ServiceRequestStatus.InProgress,
                    Category = "Cleaning",
                    Latitude = 19.1136,
                    Longitude = 72.8697,
                    LocationAddress = "71, Goregaon East, Mumbai, Maharashtra 400063",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    AcceptedAt = DateTime.UtcNow.AddDays(-2),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new ServiceRequest
                {
                    Title = "AC Not Cooling Properly",
                    Description = "Split AC unit in the bedroom is not cooling. Compressor runs but no cold air. May need gas refill or compressor check.",
                    RequestType = ServiceRequestType.Urgent,
                    Status = ServiceRequestStatus.Accepted,
                    Category = "HVAC",
                    Latitude = 19.0596,
                    Longitude = 72.8295,
                    LocationAddress = "18, Juhu, Mumbai, Maharashtra 400049",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    AcceptedAt = DateTime.UtcNow,
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new ServiceRequest
                {
                    Title = "Furniture Assembly Required",
                    Description = "Need help assembling a new wardrobe and bookshelf purchased online. All parts and hardware included. Estimated 3-4 hours of work.",
                    RequestType = ServiceRequestType.General,
                    Status = ServiceRequestStatus.Pending,
                    Category = "Carpentry",
                    Latitude = 19.0330,
                    Longitude = 72.8496,
                    LocationAddress = "305, Dadar West, Mumbai, Maharashtra 400028",
                    CreatedAt = DateTime.UtcNow,
                    UserId = demoUser.Id
                }
            };

            context.ServiceRequests.AddRange(serviceRequests);
            await context.SaveChangesAsync();
        }

        // ── Appointments ──────────────────────────────────────────
        if (!await context.Appointments.AnyAsync())
        {
            var appointments = new List<Appointment>
            {
                new Appointment
                {
                    Title = "Deep Cleaning - Final Walk-through",
                    Description = "Final inspection of the deep cleaning work at the Goregaon apartment.",
                    ScheduledDate = DateTime.UtcNow.AddDays(2),
                    TimeSlot = "10:00 AM - 12:00 PM",
                    Status = AppointmentStatus.Confirmed,
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new Appointment
                {
                    Title = "AC Servicing Appointment",
                    Description = "Technician visit to diagnose and fix the AC cooling issue. Will carry gas refill kit.",
                    ScheduledDate = DateTime.UtcNow.AddDays(3),
                    TimeSlot = "2:00 PM - 4:00 PM",
                    Status = AppointmentStatus.Scheduled,
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new Appointment
                {
                    Title = "Furniture Assembly",
                    Description = "Wardrobe and bookshelf assembly at Dadar residence.",
                    ScheduledDate = DateTime.UtcNow.AddDays(5),
                    TimeSlot = "9:00 AM - 1:00 PM",
                    Status = AppointmentStatus.Scheduled,
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                },
                new Appointment
                {
                    Title = "Plumbing Follow-up Check",
                    Description = "Follow-up visit to verify the kitchen sink repair is holding. Quick 30-minute inspection.",
                    ScheduledDate = DateTime.UtcNow.AddDays(-5),
                    TimeSlot = "11:00 AM - 11:30 AM",
                    Status = AppointmentStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                }
            };

            context.Appointments.AddRange(appointments);
            await context.SaveChangesAsync();
        }

        // ── Orders & OrderItems ───────────────────────────────────
        if (!await context.Orders.AnyAsync())
        {
            var allProducts = await context.Products.ToListAsync();

            var order1 = new Order
            {
                OrderNumber = "ORD-2026-0001",
                TotalAmount = 4798.00m,
                TaxAmount = 863.64m,
                ShippingAmount = 0.00m,
                Status = OrderStatus.Delivered,
                ShippingAddress = "202, Andheri West, Mumbai, Maharashtra 400053",
                Notes = "Please deliver before 5 PM",
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UserId = demoUser.Id
            };

            var order2 = new Order
            {
                OrderNumber = "ORD-2026-0002",
                TotalAmount = 3498.00m,
                TaxAmount = 629.64m,
                ShippingAmount = 99.00m,
                Status = OrderStatus.Shipped,
                ShippingAddress = "202, Andheri West, Mumbai, Maharashtra 400053",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UserId = demoUser.Id
            };

            var order3 = new Order
            {
                OrderNumber = "ORD-2026-0003",
                TotalAmount = 10499.00m,
                TaxAmount = 1889.82m,
                ShippingAmount = 0.00m,
                Status = OrderStatus.Confirmed,
                ShippingAddress = "202, Andheri West, Mumbai, Maharashtra 400053",
                Notes = "Ring the bell twice",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UserId = demoUser.Id
            };

            context.Orders.AddRange(order1, order2, order3);
            await context.SaveChangesAsync();

            // Order Items
            var speaker = allProducts.FirstOrDefault(p => p.SKU == "ELEC-SPK-001");
            var toolkit = allProducts.FirstOrDefault(p => p.SKU == "TOOL-KIT-001");
            var lamp = allProducts.FirstOrDefault(p => p.SKU == "HOME-LMP-001");
            var purifier = allProducts.FirstOrDefault(p => p.SKU == "HOME-PUR-001");
            var cleaningKit = allProducts.FirstOrDefault(p => p.SKU == "CLN-KIT-001");

            var orderItems = new List<OrderItem>();

            if (speaker != null)
            {
                orderItems.Add(new OrderItem
                {
                    OrderId = order1.Id,
                    ProductId = speaker.Id,
                    Quantity = 1,
                    UnitPrice = 1999.00m,
                    TotalPrice = 1999.00m
                });
            }
            if (toolkit != null)
            {
                orderItems.Add(new OrderItem
                {
                    OrderId = order1.Id,
                    ProductId = toolkit.Id,
                    Quantity = 1,
                    UnitPrice = 2799.00m,
                    TotalPrice = 2799.00m
                });
            }
            if (lamp != null)
            {
                orderItems.Add(new OrderItem
                {
                    OrderId = order2.Id,
                    ProductId = lamp.Id,
                    Quantity = 1,
                    UnitPrice = 1899.00m,
                    TotalPrice = 1899.00m
                });
            }
            if (cleaningKit != null)
            {
                orderItems.Add(new OrderItem
                {
                    OrderId = order2.Id,
                    ProductId = cleaningKit.Id,
                    Quantity = 2,
                    UnitPrice = 699.00m,
                    TotalPrice = 1398.00m
                });
            }
            if (purifier != null)
            {
                orderItems.Add(new OrderItem
                {
                    OrderId = order3.Id,
                    ProductId = purifier.Id,
                    Quantity = 1,
                    UnitPrice = 10499.00m,
                    TotalPrice = 10499.00m
                });
            }

            context.OrderItems.AddRange(orderItems);
            await context.SaveChangesAsync();

            // ── Payments (for Orders) ─────────────────────────────
            var payments = new List<Payment>
            {
                new Payment
                {
                    Amount = 4798.00m,
                    Currency = "INR",
                    Status = PaymentStatus.Completed,
                    Gateway = PaymentGateway.Razorpay,
                    TransactionId = "txn_RPay2026A001",
                    GatewayOrderId = "order_RPay2026A001",
                    GatewayPaymentId = "pay_RPay2026A001",
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    CompletedAt = DateTime.UtcNow.AddDays(-12),
                    UserId = demoUser.Id,
                    OrderId = order1.Id
                },
                new Payment
                {
                    Amount = 3498.00m,
                    Currency = "INR",
                    Status = PaymentStatus.Completed,
                    Gateway = PaymentGateway.Razorpay,
                    TransactionId = "txn_RPay2026A002",
                    GatewayOrderId = "order_RPay2026A002",
                    GatewayPaymentId = "pay_RPay2026A002",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    CompletedAt = DateTime.UtcNow.AddDays(-3),
                    UserId = demoUser.Id,
                    OrderId = order2.Id
                },
                new Payment
                {
                    Amount = 10499.00m,
                    Currency = "INR",
                    Status = PaymentStatus.Pending,
                    Gateway = PaymentGateway.Razorpay,
                    TransactionId = "txn_RPay2026A003",
                    GatewayOrderId = "order_RPay2026A003",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UserId = demoUser.Id,
                    OrderId = order3.Id
                }
            };

            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();
        }

        // ── Ratings ───────────────────────────────────────────────
        if (!await context.Ratings.AnyAsync())
        {
            // Get completed service requests for linking
            var completedRequests = await context.ServiceRequests
                .Where(sr => sr.Status == ServiceRequestStatus.Completed)
                .OrderBy(sr => sr.CreatedAt)
                .ToListAsync();

            var ratings = new List<Rating>
            {
                new Rating
                {
                    Stars = 5,
                    ReviewText = "Excellent plumbing work! Fixed the leak quickly and even cleaned up afterwards. Highly recommended.",
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id,
                    ServiceRequestId = completedRequests.ElementAtOrDefault(0)?.Id
                },
                new Rating
                {
                    Stars = 4,
                    ReviewText = "Good electrical work. Identified the faulty wiring and replaced it professionally. Took a bit longer than expected but the result is great.",
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id,
                    ServiceRequestId = completedRequests.ElementAtOrDefault(1)?.Id
                },
                new Rating
                {
                    Stars = 5,
                    ReviewText = "Very professional service provider. Arrives on time, communicates well, and does quality work. Will definitely hire again.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UserId = demoUser.Id,
                    ServiceProviderId = provider.Id
                }
            };

            context.Ratings.AddRange(ratings);
            await context.SaveChangesAsync();
        }

        // ── Feedback ──────────────────────────────────────────────
        if (!await context.Feedbacks.AnyAsync())
        {
            var feedbacks = new List<Feedback>
            {
                new Feedback
                {
                    Subject = "Bug Report: Payment Confirmation Delay",
                    Message = "After completing a Razorpay payment, the confirmation page takes about 30 seconds to load. The payment goes through but the delay is confusing. Happened twice with my recent orders.",
                    Category = "Bug Report",
                    IsResolved = true,
                    AdminResponse = "Thank you for reporting this. We have optimized the payment callback handler and this should now be resolved. Please let us know if you face the issue again.",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    ResolvedAt = DateTime.UtcNow.AddDays(-8),
                    UserId = demoUser.Id
                },
                new Feedback
                {
                    Subject = "Feature Request: Recurring Service Booking",
                    Message = "It would be great to have an option to schedule recurring services like monthly deep cleaning or quarterly pest control. Currently I have to create a new request every time.",
                    Category = "Feature Request",
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UserId = demoUser.Id
                },
                new Feedback
                {
                    Subject = "Great Platform Experience",
                    Message = "Just wanted to say that the platform is really well designed and easy to use. Found a great service provider for my plumbing issue within hours. The real-time notifications are a nice touch!",
                    Category = "General",
                    IsResolved = true,
                    AdminResponse = "Thank you so much for the positive feedback! We're glad you had a great experience. We're continuously working to make the platform even better.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    ResolvedAt = DateTime.UtcNow.AddDays(-1),
                    UserId = demoUser.Id
                }
            };

            context.Feedbacks.AddRange(feedbacks);
            await context.SaveChangesAsync();
        }

        // ── Notifications ─────────────────────────────────────────
        if (!await context.Notifications.AnyAsync())
        {
            var notifications = new List<Notification>
            {
                // For Demo User
                new Notification
                {
                    Title = "Service Request Accepted",
                    Message = "Your service request 'Kitchen Sink Leaking' has been accepted by Demo Service Provider.",
                    Type = NotificationType.RequestAccepted,
                    Link = "/UserDashboard/ServiceRequests",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-14),
                    UserId = demoUser.Id
                },
                new Notification
                {
                    Title = "Service Completed",
                    Message = "Your service request 'Kitchen Sink Leaking' has been marked as completed. Please rate the service.",
                    Type = NotificationType.RequestCompleted,
                    Link = "/UserDashboard/ServiceRequests",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-13),
                    UserId = demoUser.Id
                },
                new Notification
                {
                    Title = "Order Delivered",
                    Message = "Your order ORD-2026-0001 has been delivered successfully. Thank you for shopping with us!",
                    Type = NotificationType.OrderUpdate,
                    Link = "/UserDashboard/Orders",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UserId = demoUser.Id
                },
                // For Service Provider
                new Notification
                {
                    Title = "New Service Request",
                    Message = "A new urgent service request 'AC Not Cooling Properly' has been assigned to you.",
                    Type = NotificationType.ServiceRequest,
                    Link = "/ServiceProvider/Requests",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UserId = provider.Id
                },
                // For Shopkeeper
                new Notification
                {
                    Title = "New Order Received",
                    Message = "You have received a new order ORD-2026-0003 for Water Purifier. Please confirm and process it.",
                    Type = NotificationType.OrderUpdate,
                    Link = "/Shopkeeper/Orders",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UserId = shopkeeper.Id
                },
                // For Admin
                new Notification
                {
                    Title = "New Feedback Received",
                    Message = "A new feedback 'Feature Request: Recurring Service Booking' has been submitted by Demo User.",
                    Type = NotificationType.General,
                    Link = "/Admin/Feedback",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UserId = admin.Id
                }
            };

            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();
        }
    }
}
