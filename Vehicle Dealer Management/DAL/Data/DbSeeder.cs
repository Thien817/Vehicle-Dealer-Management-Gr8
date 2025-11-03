using Microsoft.EntityFrameworkCore;
using Vehicle_Dealer_Management.DAL.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vehicle_Dealer_Management.DAL.Data
{
    public static class DbSeeder
    {
        // Helper method để hash password
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static void SeedData(ApplicationDbContext context)
        {
            // Đảm bảo database đã được tạo
            context.Database.EnsureCreated();

            // Seed Roles nếu chưa có
            if (!context.Roles.Any())
            {
                var roles = new List<Role>
                {
                    new Role { Code = "CUSTOMER", Name = "Khách hàng", IsOperational = true },
                    new Role { Code = "DEALER_STAFF", Name = "Nhân viên đại lý", IsOperational = true },
                    new Role { Code = "DEALER_MANAGER", Name = "Quản lý đại lý", IsOperational = true },
                    new Role { Code = "EVM_STAFF", Name = "Nhân viên hãng xe", IsOperational = true },
                    new Role { Code = "EVM_ADMIN", Name = "Quản trị viên", IsOperational = true }
                };

                context.Roles.AddRange(roles);
                context.SaveChanges();
            }

            // Seed Dealers
            if (!context.Dealers.Any())
            {
                var dealers = new List<Dealer>
                {
                    new Dealer
                    {
                        Code = "DL001",
                        Name = "Đại lý Hà Nội",
                        Address = "123 Đường Láng, Đống Đa, Hà Nội",
                        PhoneNumber = "0241234567",
                        Email = "hanoi@dealer.com",
                        Status = "ACTIVE",
                        IsActive = true
                    },
                    new Dealer
                    {
                        Code = "DL002",
                        Name = "Đại lý TP.HCM",
                        Address = "456 Nguyễn Huệ, Quận 1, TP.HCM",
                        PhoneNumber = "0289876543",
                        Email = "hcm@dealer.com",
                        Status = "ACTIVE",
                        IsActive = true
                    }
                };

                context.Dealers.AddRange(dealers);
                context.SaveChanges();
            }

            // Seed Users (5 users tương ứng 5 roles)
            if (!context.Users.Any())
            {
                var roles = context.Roles.ToList();
                var dealers = context.Dealers.ToList();

                var users = new List<User>
                {
                    // Customer
                    new User
                    {
                        Email = "customer@test.com",
                        PasswordHash = HashPassword("123456"),
                        FullName = "Khách Hàng",
                        Phone = "0901234567",
                        RoleId = roles.First(r => r.Code == "CUSTOMER").Id,
                        DealerId = null,
                        CreatedAt = DateTime.UtcNow
                    },
                    // Dealer Staff
                    new User
                    {
                        Email = "dealerstaff@test.com",
                        PasswordHash = HashPassword("123456"),
                        FullName = "Dealer Staff",
                        Phone = "0902345678",
                        RoleId = roles.First(r => r.Code == "DEALER_STAFF").Id,
                        DealerId = dealers.First().Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    // Dealer Manager
                    new User
                    {
                        Email = "dealermanager@test.com",
                        PasswordHash = HashPassword("123456"),
                        FullName = "Dealer Manager",
                        Phone = "0903456789",
                        RoleId = roles.First(r => r.Code == "DEALER_MANAGER").Id,
                        DealerId = dealers.First().Id,
                        CreatedAt = DateTime.UtcNow
                    },
                    // EVM Staff
                    new User
                    {
                        Email = "evmstaff@test.com",
                        PasswordHash = HashPassword("123456"),
                        FullName = "EVM Staff",
                        Phone = "0904567890",
                        RoleId = roles.First(r => r.Code == "EVM_STAFF").Id,
                        DealerId = null,
                        CreatedAt = DateTime.UtcNow
                    },
                    // EVM Admin
                    new User
                    {
                        Email = "admin@test.com",
                        PasswordHash = HashPassword("123456"),
                        FullName = "Admin",
                        Phone = "0905678901",
                        RoleId = roles.First(r => r.Code == "EVM_ADMIN").Id,
                        DealerId = null,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // Seed Vehicles (3 vehicles)
            if (!context.Vehicles.Any())
            {
                var vehicles = new List<Vehicle>
                {
                    new Vehicle
                    {
                        ModelName = "Model S",
                        VariantName = "Premium",
                        SpecJson = @"{
                            ""battery"": ""100kWh"",
                            ""range"": ""610km"",
                            ""power"": ""670hp"",
                            ""acceleration"": ""3.1s (0-100km/h)"",
                            ""maxSpeed"": ""250km/h""
                        }",
                        ImageUrl = "https://images.unsplash.com/photo-1617788138017-80ad40651399?w=800",
                        Status = "AVAILABLE",
                        CreatedDate = DateTime.UtcNow
                    },
                    new Vehicle
                    {
                        ModelName = "Model 3",
                        VariantName = "Standard",
                        SpecJson = @"{
                            ""battery"": ""60kWh"",
                            ""range"": ""420km"",
                            ""power"": ""283hp"",
                            ""acceleration"": ""5.3s (0-100km/h)"",
                            ""maxSpeed"": ""225km/h""
                        }",
                        ImageUrl = "https://images.unsplash.com/photo-1617531653332-bd46c24f2068?w=800",
                        Status = "AVAILABLE",
                        CreatedDate = DateTime.UtcNow
                    },
                    new Vehicle
                    {
                        ModelName = "Model X",
                        VariantName = "Performance",
                        SpecJson = @"{
                            ""battery"": ""100kWh"",
                            ""range"": ""576km"",
                            ""power"": ""780hp"",
                            ""acceleration"": ""2.6s (0-100km/h)"",
                            ""maxSpeed"": ""262km/h""
                        }",
                        ImageUrl = "https://giaxeoto.vn/admin/upload/images/resize/640-van-hanh-xe-tesla-model-x.jpg",
                        Status = "AVAILABLE",
                        CreatedDate = DateTime.UtcNow
                    }
                };

                context.Vehicles.AddRange(vehicles);
                context.SaveChanges();
            }

            // Seed Price Policies
            if (!context.PricePolicies.Any())
            {
                var vehicles = context.Vehicles.ToList();
                var dealers = context.Dealers.ToList();

                var pricePolicies = new List<PricePolicy>
                {
                    // Global price cho Model S
                    new PricePolicy
                    {
                        VehicleId = vehicles[0].Id,
                        DealerId = null,
                        Msrp = 2500000000, // 2.5 tỷ (giá cuối, không có discount nên = giá gốc)
                        WholesalePrice = 2300000000, // 2.3 tỷ (giá cuối, không có discount nên = giá gốc)
                        OriginalMsrp = 2500000000, // 2.5 tỷ (giá gốc)
                        OriginalWholesalePrice = 2300000000, // 2.3 tỷ (giá sỉ gốc)
                        Note = "Giá niêm yết chính thức từ nhà sản xuất. Giá có thể thay đổi tùy theo khuyến mãi và đại lý. Vui lòng liên hệ đại lý để biết giá chính xác nhất.",
                        ValidFrom = DateTime.UtcNow.AddMonths(-1),
                        ValidTo = null,
                        CreatedDate = DateTime.UtcNow
                    },
                    // Global price cho Model 3
                    new PricePolicy
                    {
                        VehicleId = vehicles[1].Id,
                        DealerId = null,
                        Msrp = 1500000000, // 1.5 tỷ (giá cuối, không có discount nên = giá gốc)
                        WholesalePrice = 1400000000, // 1.4 tỷ (giá cuối, không có discount nên = giá gốc)
                        OriginalMsrp = 1500000000, // 1.5 tỷ (giá gốc)
                        OriginalWholesalePrice = 1400000000, // 1.4 tỷ (giá sỉ gốc)
                        Note = "Giá đã bao gồm VAT. Hiện đang có chương trình khuyến mãi hấp dẫn cho khách hàng đặt mua trong tháng này. Giá có thể thay đổi tùy theo đại lý.",
                        ValidFrom = DateTime.UtcNow.AddMonths(-1),
                        ValidTo = null,
                        CreatedDate = DateTime.UtcNow
                    },
                    // Global price cho Model X
                    new PricePolicy
                    {
                        VehicleId = vehicles[2].Id,
                        DealerId = null,
                        Msrp = 3500000000, // 3.5 tỷ (giá cuối, không có discount nên = giá gốc)
                        WholesalePrice = 3300000000, // 3.3 tỷ (giá cuối, không có discount nên = giá gốc)
                        OriginalMsrp = 3500000000, // 3.5 tỷ (giá gốc)
                        OriginalWholesalePrice = 3300000000, // 3.3 tỷ (giá sỉ gốc)
                        Note = "Giá niêm yết cho phiên bản Performance cao cấp. Bao gồm đầy đủ phụ kiện tiêu chuẩn. Hỗ trợ trả góp lãi suất ưu đãi từ các ngân hàng đối tác.",
                        ValidFrom = DateTime.UtcNow.AddMonths(-1),
                        ValidTo = null,
                        CreatedDate = DateTime.UtcNow
                    }
                };

                context.PricePolicies.AddRange(pricePolicies);
                context.SaveChanges();
            }
            else
            {
                // Update existing PricePolicies to have OriginalMsrp and OriginalWholesalePrice
                // Nếu chưa có giá gốc, set giá cuối làm giá gốc (backward compatibility)
                var policiesWithoutOriginal = context.PricePolicies
                    .Where(p => !p.OriginalMsrp.HasValue)
                    .ToList();

                foreach (var policy in policiesWithoutOriginal)
                {
                    policy.OriginalMsrp = policy.Msrp;
                    if (policy.WholesalePrice.HasValue)
                    {
                        policy.OriginalWholesalePrice = policy.WholesalePrice.Value;
                    }
                    else
                    {
                        policy.OriginalWholesalePrice = null;
                    }
                }

                if (policiesWithoutOriginal.Any())
                {
                    context.SaveChanges();
                }
            }

            // Seed Stocks (EVM stock)
            if (!context.Stocks.Any())
            {
                var vehicles = context.Vehicles.ToList();

                var stocks = new List<Stock>
                {
                    // EVM stock
                    new Stock
                    {
                        OwnerType = "EVM",
                        OwnerId = 0, // 0 = EVM
                        VehicleId = vehicles[0].Id,
                        ColorCode = "BLACK",
                        Name = $"{vehicles[0].ModelName} {vehicles[0].VariantName}",
                        Qty = 10,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Stock
                    {
                        OwnerType = "EVM",
                        OwnerId = 0,
                        VehicleId = vehicles[0].Id,
                        ColorCode = "WHITE",
                        Name = $"{vehicles[0].ModelName} {vehicles[0].VariantName}",
                        Qty = 8,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Stock
                    {
                        OwnerType = "EVM",
                        OwnerId = 0,
                        VehicleId = vehicles[1].Id,
                        ColorCode = "BLACK",
                        Name = $"{vehicles[1].ModelName} {vehicles[1].VariantName}",
                        Qty = 15,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Stock
                    {
                        OwnerType = "EVM",
                        OwnerId = 0,
                        VehicleId = vehicles[1].Id,
                        ColorCode = "RED",
                        Name = $"{vehicles[1].ModelName} {vehicles[1].VariantName}",
                        Qty = 12,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Stock
                    {
                        OwnerType = "EVM",
                        OwnerId = 0,
                        VehicleId = vehicles[2].Id,
                        ColorCode = "BLACK",
                        Name = $"{vehicles[2].ModelName} {vehicles[2].VariantName}",
                        Qty = 5,
                        CreatedDate = DateTime.UtcNow
                    }
                };

                context.Stocks.AddRange(stocks);
                context.SaveChanges();
            }
            else
            {
                // Update existing Stocks to have Name field filled from Vehicle
                var stocksWithoutName = context.Stocks
                    .Where(s => string.IsNullOrEmpty(s.Name))
                    .ToList();

                foreach (var stock in stocksWithoutName)
                {
                    var vehicle = context.Vehicles.Find(stock.VehicleId);
                    if (vehicle != null)
                    {
                        stock.Name = $"{vehicle.ModelName} {vehicle.VariantName}";
                    }
                }

                if (stocksWithoutName.Any())
                {
                    context.SaveChanges();
                }
            }

            // Seed Customer Profiles
            if (!context.CustomerProfiles.Any())
            {
                var customers = new List<CustomerProfile>
                {
                    new CustomerProfile
                    {
                        FullName = "Nguyễn Văn Khách",
                        Phone = "0901234567",
                        Email = "customer@test.com",
                        Address = "123 Đường ABC, Quận 1, TP.HCM",
                        CreatedDate = DateTime.UtcNow
                    },
                    new CustomerProfile
                    {
                        FullName = "Trần Thị Mai",
                        Phone = "0907654321",
                        Email = "mai.tran@example.com",
                        Address = "456 Đường XYZ, Quận 2, TP.HCM",
                        CreatedDate = DateTime.UtcNow
                    }
                };

                context.CustomerProfiles.AddRange(customers);
                context.SaveChanges();
            }

            // Seed Promotions (optional)
            if (!context.Promotions.Any())
            {
                var promotions = new List<Promotion>
                {
                    new Promotion
                    {
                        Name = "Giảm giá cuối năm",
                        Scope = "GLOBAL",
                        RuleJson = @"{""discountPercent"": 10}",
                        ValidFrom = DateTime.UtcNow,
                        ValidTo = DateTime.UtcNow.AddMonths(1),
                        CreatedDate = DateTime.UtcNow
                    },
                    new Promotion
                    {
                        Name = "Khuyến mãi đặc biệt",
                        Scope = "GLOBAL",
                        RuleJson = @"{""discountPercent"": 20}",
                        ValidFrom = DateTime.UtcNow,
                        ValidTo = DateTime.UtcNow.AddMonths(2),
                        CreatedDate = DateTime.UtcNow
                    }
                };

                context.Promotions.AddRange(promotions);
                context.SaveChanges();
            }

            // Seed Notifications - Chỉ tạo dữ liệu mẫu tối thiểu
            // Notifications thực tế sẽ được tạo tự động từ business logic:
            // - Khi tạo PricePolicy với Promotion (trong PricePolicies.cshtml.cs)
            // - Khi có các events khác trong tương lai
            // 
            // Phần này chỉ tạo 1 notification chào mừng mẫu để test UI, 
            // KHÔNG nên hard code notifications về promotions vì chúng sẽ được tạo tự động
            if (!context.Notifications.Any())
            {
                var customerRole = context.Roles.FirstOrDefault(r => r.Code == "CUSTOMER");
                
                if (customerRole != null)
                {
                    var customerUsers = context.Users
                        .Where(u => u.RoleId == customerRole.Id)
                        .ToList();

                    if (customerUsers.Any())
                    {
                        // Chỉ tạo notification chào mừng đơn giản để test UI
                        // Notifications về promotions sẽ được tạo tự động khi EVM staff tạo PricePolicy với Promotion
                        var welcomeNotifications = customerUsers.Select(customer => new Notification
                        {
                            UserId = customer.Id,
                            Title = "Chào mừng bạn đến với EVM Dealer Portal!",
                            Content = "Khám phá các mẫu xe điện mới nhất và các chương trình khuyến mãi hấp dẫn.",
                            Type = "INFO",
                            LinkUrl = "/Customer/Vehicles",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow.AddDays(-1)
                        }).ToList();

                        context.Notifications.AddRange(welcomeNotifications);
                        context.SaveChanges();
                    }
                }
            }

            // Seed sample Deliveries với các trạng thái khác nhau để test workflow
            // Lưu ý: Delivery thường được tạo tự động khi dealer staff lên lịch giao xe
            // Nhưng để test, tạo một vài mẫu với các trạng thái khác nhau
            if (!context.Deliveries.Any() && context.SalesDocuments.Any(sd => sd.Type == "ORDER"))
            {
                var orders = context.SalesDocuments
                    .Where(sd => sd.Type == "ORDER" && sd.Status != "DELIVERED")
                    .Take(3)
                    .ToList();

                if (orders.Any())
                {
                    var deliveries = new List<Delivery>();
                    var now = DateTime.UtcNow;

                    // Delivery 1: SCHEDULED (Đã lên lịch - chờ giao xe)
                    if (orders.Count > 0)
                    {
                        deliveries.Add(new Delivery
                        {
                            SalesDocumentId = orders[0].Id,
                            ScheduledDate = now.AddDays(7),
                            Status = "SCHEDULED",
                            CustomerConfirmed = false, // Field mới
                            CustomerConfirmedDate = null, // Field mới
                            CreatedDate = now.AddDays(-2)
                        });
                    }

                    // Delivery 2: IN_TRANSIT (Đang giao xe) - chưa customer confirm
                    if (orders.Count > 1)
                    {
                        deliveries.Add(new Delivery
                        {
                            SalesDocumentId = orders[1].Id,
                            ScheduledDate = now.AddDays(5),
                            Status = "IN_TRANSIT",
                            CustomerConfirmed = false, // Field mới - chưa confirm
                            CustomerConfirmedDate = null, // Field mới
                            CreatedDate = now.AddDays(-5)
                        });
                    }

                    // Delivery 3: IN_TRANSIT (Đang giao xe) - đã customer confirm
                    if (orders.Count > 2)
                    {
                        deliveries.Add(new Delivery
                        {
                            SalesDocumentId = orders[2].Id,
                            ScheduledDate = now.AddDays(3),
                            Status = "IN_TRANSIT",
                            CustomerConfirmed = true, // Field mới - đã confirm
                            CustomerConfirmedDate = now.AddDays(-1), // Field mới
                            CreatedDate = now.AddDays(-7)
                        });
                    }

                    if (deliveries.Any())
                    {
                        context.Deliveries.AddRange(deliveries);
                        context.SaveChanges();

                        // Update SalesDocument status tương ứng
                        foreach (var delivery in deliveries)
                        {
                            var order = orders.FirstOrDefault(o => o.Id == delivery.SalesDocumentId);
                            if (order != null)
                            {
                                if (delivery.Status == "SCHEDULED")
                                {
                                    order.Status = "DELIVERY_SCHEDULED";
                                }
                                else if (delivery.Status == "IN_TRANSIT")
                                {
                                    order.Status = "IN_DELIVERY";
                                }
                                order.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        context.SaveChanges();
                    }
                }
            }

            // Update existing Deliveries để đảm bảo tính nhất quán dữ liệu với các field mới
            // Delivery mới sẽ tự động có CustomerConfirmed = false (default trong model)
            var deliveriesToFix = context.Deliveries
                .Where(d => (!d.CustomerConfirmed && d.CustomerConfirmedDate.HasValue) || // CustomerConfirmedDate phải null nếu CustomerConfirmed = false
                           (d.Status == "SCHEDULED" && d.CustomerConfirmed)) // SCHEDULED không thể đã confirmed
                .ToList();

            foreach (var delivery in deliveriesToFix)
            {
                if (delivery.Status == "SCHEDULED" && delivery.CustomerConfirmed)
                {
                    // Reset confirmation nếu status là SCHEDULED (chưa bắt đầu giao thì không thể đã confirm)
                    delivery.CustomerConfirmed = false;
                    delivery.CustomerConfirmedDate = null;
                }
                else if (!delivery.CustomerConfirmed && delivery.CustomerConfirmedDate.HasValue)
                {
                    // Xóa confirmed date nếu chưa confirmed
                    delivery.CustomerConfirmedDate = null;
                }
            }

            if (deliveriesToFix.Any())
            {
                context.SaveChanges();
            }

            // Seed sample Reviews (Type = REVIEW) nếu có orders đã DELIVERED
            // Reviews chỉ có thể được tạo cho orders đã hoàn thành (DELIVERED và đã thanh toán đủ 100%)
            if (!context.Feedbacks.Any(f => f.Type == "REVIEW"))
            {
                var orders = context.SalesDocuments
                    .Where(sd => sd.Type == "ORDER" && sd.Status == "DELIVERED")
                    .Include(sd => sd.Customer)
                    .Include(sd => sd.Dealer)
                    .Take(2) // Chỉ tạo 2 reviews mẫu
                    .ToList();

                var customers = context.CustomerProfiles.ToList();
                var dealers = context.Dealers.ToList();

                if (orders.Any() && customers.Any() && dealers.Any())
                {
                    var reviews = new List<Feedback>();

                    // Review 1: 5 sao
                    if (orders.Count > 0)
                    {
                        var order1 = orders[0];
                        var customer1 = customers.FirstOrDefault(c => c.Id == order1.CustomerId);
                        var dealer1 = dealers.FirstOrDefault(d => d.Id == order1.DealerId);

                        if (customer1 != null && dealer1 != null)
                        {
                            reviews.Add(new Feedback
                            {
                                Type = "REVIEW",
                                Status = "RESOLVED", // Reviews không dùng workflow status
                                CustomerId = customer1.Id,
                                DealerId = dealer1.Id,
                                OrderId = order1.Id,
                                Rating = 5,
                                Content = "Dịch vụ rất tốt! Nhân viên tư vấn nhiệt tình, xe chất lượng cao. Giao hàng đúng hẹn. Rất hài lòng!",
                                CreatedAt = DateTime.UtcNow.AddDays(-5),
                                UpdatedAt = null,
                                ResolvedAt = null
                            });
                        }
                    }

                    // Review 2: 4 sao
                    if (orders.Count > 1)
                    {
                        var order2 = orders[1];
                        var customer2 = customers.FirstOrDefault(c => c.Id == order2.CustomerId);
                        var dealer2 = dealers.FirstOrDefault(d => d.Id == order2.DealerId);

                        if (customer2 != null && dealer2 != null)
                        {
                            reviews.Add(new Feedback
                            {
                                Type = "REVIEW",
                                Status = "RESOLVED",
                                CustomerId = customer2.Id,
                                DealerId = dealer2.Id,
                                OrderId = order2.Id,
                                Rating = 4,
                                Content = "Xe đẹp, chất lượng tốt. Nhưng thời gian giao hàng hơi chậm một chút. Nhìn chung là hài lòng.",
                                CreatedAt = DateTime.UtcNow.AddDays(-3),
                                UpdatedAt = null,
                                ResolvedAt = null
                            });
                        }
                    }

                    if (reviews.Any())
                    {
                        context.Feedbacks.AddRange(reviews);
                        context.SaveChanges();
                    }
                }
            }
        }
    }
}

