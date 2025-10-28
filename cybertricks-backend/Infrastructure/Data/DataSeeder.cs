using ct.backend.Domain.Entities;
using ct.backend.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ct.backend.Infrastructure.Data
{
    public class DatabaseSeeder
    {
        private readonly BookingDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(
            BookingDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> SeedAllAsync()
        {
            // await _context.Database.MigrateAsync();

            await SeedBrandsAsync();
            await SeedStoresAsync();
            await SeedFloorsAsync();
            await SeedRoomsAsync();         // có set HourlyRate
            await SeedMachinesAsync();
            await SeedMenuCategoriesAsync();
            await SeedMenuItemsAsync();

            await SeedRolesAsync();         // fix NormalizedName
            await SeedUsersAsync();         // thống nhất username lowercase
            await SeedBrandOwnersAsync();
            await SeedStoreManagersAsync(); // dùng entity StoreManager
            await SeedStoreStaffsAsync();   // dùng entity StoreStaff
            await SeedStoreAccountsAsync(); // tài khoản ngân hàng cửa hàng

            await SeedPricingRulesAsync();  // GIỜ CAO ĐIỂM/THẤP ĐIỂM/WEEKEND
            await SeedVouchersAsync();      // voucher hệ thống + theo store

            await SeedSampleBookingsAsync();// (tuỳ chọn) 1-2 booking demo + BookingMachine

            return true;
        }

        // ===================== 1) BRANDS =====================
        private static readonly (string Code, string Name, string Email, string Phone, string Desc, bool IsLarge)[] BrandData =
        {
            ("CYBERWAVE",  "CyberWave Esports",  "contact@cyberwave.vn", "0901234567",
                "Chuỗi phòng game cao cấp, định hướng eSports.", true),
            ("PIXELFORGE", "PixelForge Gaming",  "hello@pixelforge.vn",  "0907654321",
                "Chuỗi phòng game quy mô vừa và nhỏ, giá dễ tiếp cận.", false)
        };

        private async Task SeedBrandsAsync()
        {
            foreach (var b in BrandData)
            {
                var exists = await _context.Brands.AnyAsync(x => x.Code == b.Code);
                if (exists) continue;

                await _context.Brands.AddAsync(new Brand
                {
                    Code = b.Code,
                    Name = b.Name,
                    ContactEmail = b.Email,
                    ContactPhone = b.Phone,
                    Description = b.Desc,
                    Status = BrandStatus.active,
                    AvgRating = b.IsLarge ? 4.6 : 4.2,
                    RatingCount = b.IsLarge ? 128 : 74
                });
            }
            await _context.SaveChangesAsync();
        }

        // ===================== 2) STORES =====================
        private sealed record StoreSeed(string BrandCode, string Name, string Address, bool IsLarge);
        private static readonly StoreSeed[] StoreData =
        {
            new("CYBERWAVE", "CyberWave Q1 Flagship", "12 Nguyễn Huệ, Q1, TP.HCM", true),
            new("CYBERWAVE", "CyberWave Q7",          "99 Nguyễn Văn Linh, Q7, TP.HCM", false),
            new("PIXELFORGE","PixelForge Đà Nẵng",     "50 Bạch Đằng, Hải Châu, Đà Nẵng", false),
            new("PIXELFORGE","PixelForge Hà Nội",      "210 Xã Đàn, Đống Đa, Hà Nội", true),
        };

        private async Task SeedStoresAsync()
        {
            var brands = await _context.Brands.ToListAsync();
            foreach (var s in StoreData)
            {
                var brand = brands.First(b => b.Code == s.BrandCode);
                var exists = await _context.Stores.AnyAsync(x => x.Name == s.Name && x.BrandId == brand.BrandId);
                if (exists) continue;

                await _context.Stores.AddAsync(new Store
                {
                    BrandId = brand.BrandId,
                    Name = s.Name,
                    Address = s.Address,
                    ContactPhone = "1900-1234",
                    Status = StoreStatus.active,
                    DisplayOrder = s.IsLarge ? 1 : 2
                });
            }
            await _context.SaveChangesAsync();
        }

        // ===================== 3) FLOORS =====================
        private sealed record FloorSeed(string StoreName, int FloorNumber, string? Name);
        private static readonly FloorSeed[] FloorData =
        {
            new("CyberWave Q1 Flagship", 1, "Tầng 1 (Tiếp tân)"),
            new("CyberWave Q1 Flagship", 2, "Tầng 2 (Luyện tập)"),
            new("CyberWave Q1 Flagship", 3, "Tầng 3 (Thi đấu)"),
            new("CyberWave Q7", 1, "Tầng trệt"),
            new("CyberWave Q7", 2, "Lầu 1"),
            new("PixelForge Đà Nẵng", 1, "Tầng 1"),
            new("PixelForge Đà Nẵng", 2, "Tầng 2"),
            new("PixelForge Hà Nội", 1, "Tầng 1"),
            new("PixelForge Hà Nội", 2, "Tầng 2"),
            new("PixelForge Hà Nội", 3, "Tầng 3"),
        };

        private async Task SeedFloorsAsync()
        {
            var stores = await _context.Stores.ToListAsync();
            foreach (var f in FloorData)
            {
                var store = stores.First(s => s.Name == f.StoreName);
                var exists = await _context.Floors.AnyAsync(x => x.StoreId == store.StoreId && x.FloorNumber == f.FloorNumber);
                if (exists) continue;

                await _context.Floors.AddAsync(new Floor
                {
                    StoreId = store.StoreId,
                    FloorNumber = f.FloorNumber,
                    Name = f.Name,
                    Status = FloorStatus.active,
                    DisplayOrder = f.FloorNumber
                });
            }
            await _context.SaveChangesAsync();
        }

        // ===================== 4) ROOMS =====================
        private sealed record RoomSeed(string StoreName, int FloorNumber, string RoomName, RoomType Type, int MachineCount, bool IsVip);
        private static readonly RoomSeed[] RoomData =
        {
            new("CyberWave Q1 Flagship", 1, "CW1-STD-01", RoomType.normal, 8, false),
            new("CyberWave Q1 Flagship", 1, "CW1-STD-02", RoomType.normal, 8, false),
            new("CyberWave Q1 Flagship", 2, "CW1-VIP-01", RoomType.vip,      6, true),
            new("CyberWave Q1 Flagship", 2, "CW1-STD-03", RoomType.normal, 10, false),
            new("CyberWave Q1 Flagship", 3, "CW1-ARENA",  RoomType.vip,     10, true),

            new("CyberWave Q7", 1, "CW7-STD-01", RoomType.normal, 7, false),
            new("CyberWave Q7", 1, "CW7-VIP-01", RoomType.vip,      5, true),
            new("CyberWave Q7", 2, "CW7-STD-02", RoomType.normal, 6, false),

            new("PixelForge Đà Nẵng", 1, "PFDN-STD-01", RoomType.normal, 8, false),
            new("PixelForge Đà Nẵng", 2, "PFDN-VIP-01", RoomType.vip,      5, true),
            new("PixelForge Đà Nẵng", 2, "PFDN-STD-02", RoomType.normal, 6, false),

            new("PixelForge Hà Nội", 1, "PFHN-STD-01", RoomType.normal, 9, false),
            new("PixelForge Hà Nội", 2, "PFHN-STD-02", RoomType.normal, 9, false),
            new("PixelForge Hà Nội", 2, "PFHN-VIP-01", RoomType.vip,      6, true),
            new("PixelForge Hà Nội", 3, "PFHN-VIP-ARENA", RoomType.vip,   8, true),
        };

        private async Task SeedRoomsAsync()
        {
            // Map (StoreName,FloorNumber) -> FloorId
            var floors = await _context.Floors.Include(f => f.Store).ToListAsync();

            foreach (var r in RoomData)
            {
                var floor = floors.First(f => f.Store!.Name == r.StoreName && f.FloorNumber == r.FloorNumber);
                var exists = await _context.Rooms.AnyAsync(x => x.FloorId == floor.FloorId && x.Name == r.RoomName);
                if (exists) continue;

                await _context.Rooms.AddAsync(new Room
                {
                    FloorId = floor.FloorId,
                    Name = r.RoomName,
                    Type = r.Type,
                    Capacity = r.IsVip ? 10 : 8,
                    HourlyRate = r.IsVip ? 30000 : 20000, // <— set mặc định
                    Status = RoomStatus.active,
                    DisplayOrder = r.IsVip ? 1 : 2
                });
            }
            await _context.SaveChangesAsync();
        }

        // ===================== 5) MACHINES =====================
        private async Task SeedMachinesAsync()
        {
            var rooms = await _context.Rooms.ToListAsync();

            foreach (var r in RoomData)
            {
                var room = rooms.First(x => x.Name == r.RoomName);
                var current = await _context.Machines.CountAsync(m => m.RoomId == room.RoomId);
                if (current >= r.MachineCount) continue;

                var need = r.MachineCount - current;
                for (int i = current + 1; i <= current + need; i++)
                {
                    var code = $"{r.RoomName}-PC-{i:D2}";
                    var spec = r.IsVip ? BuildSpecJsonHighEnd(i) : BuildSpecJsonStandard(i);

                    var exists = await _context.Machines.AnyAsync(m => m.Code == code);
                    if (exists) continue;

                    await _context.Machines.AddAsync(new Machine
                    {
                        RoomId = room.RoomId,
                        Code = code,
                        //Status = MachineStatus.available,
                        SpecJson = spec
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private static string BuildSpecJsonHighEnd(int index)
        {
            var spec = new
            {
                cpu = "Intel Core i7-12700F",
                gpu = "NVIDIA GeForce RTX 4070 12GB",
                ram = "32GB DDR4-3600",
                storage = new[] { "1TB NVMe SSD" },
                monitor = "27\" 2K 165Hz IPS",
                peripherals = new
                {
                    keyboard = "Mechanical TKL (RGB)",
                    mouse = "Logitech G Pro / Razer Viper",
                    headset = "HyperX Cloud II",
                    chair = "Ghế công thái học cao cấp"
                },
                os = "Windows 11 Pro",
                extras = new[] { "Bàn rộng", "Đèn ambient RGB", "Cách âm tốt" },
                note = $"VIP Rig #{index}"
            };
            return JsonSerializer.Serialize(spec);
        }

        private static string BuildSpecJsonStandard(int index)
        {
            var spec = new
            {
                cpu = "Intel Core i5-12400F",
                gpu = "NVIDIA GeForce RTX 3060 12GB",
                ram = "16GB DDR4-3200",
                storage = new[] { "512GB NVMe SSD" },
                monitor = "24\" 1080p 144Hz",
                peripherals = new
                {
                    keyboard = "Mem-Mechanical (RGB)",
                    mouse = "Logitech G102",
                    headset = "Onikuma K9",
                    chair = "Ghế lưng cao"
                },
                os = "Windows 11 Home",
                extras = new[] { "Bàn vừa", "Đèn nền cơ bản" },
                note = $"Standard Rig #{index}"
            };
            return JsonSerializer.Serialize(spec);
        }

        // ===================== 6) MENU CATEGORIES =====================
        private sealed record CatSeed(string BrandCode, string Name);
        private static readonly CatSeed[] CatData =
        {
            new("CYBERWAVE", "Combo"),
            new("CYBERWAVE", "Đồ uống"),
            new("CYBERWAVE", "Snack"),
            new("PIXELFORGE", "Combo"),
            new("PIXELFORGE", "Đồ uống"),
            new("PIXELFORGE", "Snack"),
        };

        private async Task SeedMenuCategoriesAsync()
        {
            var brands = await _context.Brands.ToListAsync();

            foreach (var c in CatData)
            {
                var brand = brands.First(b => b.Code == c.BrandCode);
                var exists = await _context.MenuCategories.AnyAsync(x => x.BrandId == brand.BrandId && x.Name == c.Name);
                if (exists) continue;

                await _context.MenuCategories.AddAsync(new MenuCategory
                {
                    BrandId = brand.BrandId,
                    Name = c.Name,
                    Active = true
                });
            }
            await _context.SaveChangesAsync();
        }

        // ===================== 7) MENU ITEMS =====================
        private sealed record ItemSeed(string BrandCode, string CategoryName, string ItemName, decimal Price);
        private static readonly ItemSeed[] ItemData =
        {
            // CYBERWAVE
            new("CYBERWAVE","Combo",    "Combo Năng Lượng (RedBull + Snack)", 49000),
            new("CYBERWAVE","Combo",    "Combo Try–Hard (Cà phê sữa + Mì ly)", 59000),
            new("CYBERWAVE","Đồ uống",  "Cà phê sữa đá", 25000),
            new("CYBERWAVE","Đồ uống",  "Trà đào cam sả", 35000),
            new("CYBERWAVE","Đồ uống",  "Nước suối", 10000),
            new("CYBERWAVE","Snack",    "Mì ly cay", 19000),
            new("CYBERWAVE","Snack",    "Khoai tây chiên", 29000),

            // PIXELFORGE
            new("PIXELFORGE","Combo",   "Combo Sinh viên (Trà chanh + Snack)", 39000),
            new("PIXELFORGE","Đồ uống", "Trà chanh đào", 22000),
            new("PIXELFORGE","Đồ uống", "Soda bạc hà", 28000),
            new("PIXELFORGE","Snack",   "Xúc xích nướng", 25000),
            new("PIXELFORGE","Snack",   "Bánh mì pate", 20000),
        };

        private async Task SeedMenuItemsAsync()
        {
            var brands = await _context.Brands.ToListAsync();
            var cats = await _context.MenuCategories.ToListAsync();

            foreach (var it in ItemData)
            {
                var brand = brands.First(b => b.Code == it.BrandCode);
                var cat = cats.First(c => c.BrandId == brand.BrandId && c.Name == it.CategoryName);

                var exists = await _context.MenuItems.AnyAsync(x => x.BrandId == brand.BrandId && x.CategoryId == cat.CategoryId && x.Name == it.ItemName);
                if (exists) continue;

                await _context.MenuItems.AddAsync(new MenuItem
                {
                    BrandId = brand.BrandId,
                    CategoryId = cat.CategoryId,
                    Name = it.ItemName,
                    Price = it.Price,
                    Active = true,
                });
            }
            await _context.SaveChangesAsync();
        }

        // ===================== 8) ROLES & USERS =====================
        private async Task SeedRolesAsync()
        {
            if (!await _roleManager.Roles.AnyAsync())
            {
                var roles = new List<IdentityRole>
                {
                    new IdentityRole { Name = "Admin",   NormalizedName = "ADMIN" },
                    new IdentityRole { Name = "Owner",   NormalizedName = "OWNER" },
                    new IdentityRole { Name = "Manager", NormalizedName = "MANAGER" }, // FIX
                    new IdentityRole { Name = "Staff",   NormalizedName = "STAFF" },
                    new IdentityRole { Name = "User",    NormalizedName = "USER" },
                };

                foreach (var role in roles)
                    await _roleManager.CreateAsync(role);
            }
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        private async Task SeedUsersAsync()
        {
            if (!await _userManager.Users.AnyAsync())
            {
                var users = new List<(User user, string password, string role)>
                {
                    (new User
                    {
                        UserName = "admin",
                        Email = "admin@gmail.com",
                        FullName = "System Administrator",
                        EmailConfirmed = true,
                        SubscriptionType = "Premium",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    }, "Abcd1234!", "Admin"),

                    (new User
                    {
                        UserName = "owner1",
                        Email = "owner1@gmail.com",
                        FullName = "Owner 1",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "Owner"),

                    (new User
                    {
                        UserName = "owner2",
                        Email = "owner2@gmail.com",
                        FullName = "Owner 2",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "Owner"),

                    (new User
                    {
                        UserName = "manager1",
                        Email = "manager1@gmail.com",
                        FullName = "Manager 1",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "Manager"),

                    (new User
                    {
                        UserName = "manager2",
                        Email = "manager2@gmail.com",
                        FullName = "Manager 2",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "Manager"),

                    (new User
                    {
                        UserName = "staff1",
                        Email = "staff1@gmail.com",
                        FullName = "Staff 1",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "Staff"),

                    (new User
                    {
                        UserName = "staff2",
                        Email = "staff2@gmail.com",
                        FullName = "Staff 2",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "Staff"),

                    (new User
                    {
                        UserName = "user1",
                        Email = "user1@gmail.com",
                        FullName = "User 1",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "User"),

                    (new User
                    {
                        UserName = "user2",
                        Email = "user2@gmail.com",
                        FullName = "User 2",
                        EmailConfirmed = true,
                        SubscriptionType = "Basic",
                        SubscriptionStartDate = DateTime.UtcNow,
                        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    }, "Abcd1234!", "User"),
                };

                foreach (var (user, password, role) in users)
                {
                    var result = await _userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                        await _userManager.AddToRoleAsync(user, role);
                }
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }

        private async Task<User?> FindUserAsync(string usernameLower) =>
            await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == usernameLower);

        private async Task SeedBrandOwnersAsync()
        {
            var cyberwave = await _context.Brands.FirstAsync(c => c.Code == "CYBERWAVE");
            var pixelforge = await _context.Brands.FirstAsync(c => c.Code == "PIXELFORGE");

            var owner1 = await FindUserAsync("owner1");
            var owner2 = await FindUserAsync("owner2");

            if (!await _context.BrandOwners.AnyAsync())
            {
                var brandOwners = new List<BrandOwner>
                {
                    new BrandOwner { BrandId = cyberwave.BrandId, UserId = owner1!.Id },
                    new BrandOwner { BrandId = pixelforge.BrandId, UserId = owner2!.Id },
                };

                await _context.BrandOwners.AddRangeAsync(brandOwners);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedStoreManagersAsync()
        {
            if (await _context.Set<StoreManager>().AnyAsync()) return;

            var manager1 = await FindUserAsync("manager1");
            var manager2 = await FindUserAsync("manager2");

            var cyberwaveStores = await _context.Stores.Where(s => s.Brand.Code == "CYBERWAVE").ToListAsync();
            var pixelforgeStores = await _context.Stores.Where(s => s.Brand.Code == "PIXELFORGE").ToListAsync();

            var managers = new List<StoreManager>();
            foreach (var s in cyberwaveStores)
                managers.Add(new StoreManager { StoreId = s.StoreId, UserId = manager1!.Id, IsPrimary = true });

            foreach (var s in pixelforgeStores)
                managers.Add(new StoreManager { StoreId = s.StoreId, UserId = manager2!.Id, IsPrimary = true });

            await _context.AddRangeAsync(managers);
            await _context.SaveChangesAsync();
        }

        private async Task SeedStoreStaffsAsync()
        {
            if (await _context.StoreStaffs.AnyAsync()) return;

            var staff1 = await FindUserAsync("staff1");
            var staff2 = await FindUserAsync("staff2");

            var anyCyberwaveStore = await _context.Stores.Where(s => s.Brand.Code == "CYBERWAVE").FirstAsync();
            var anyPixelforgeStore = await _context.Stores.Where(s => s.Brand.Code == "PIXELFORGE").FirstAsync();

            var storeStaffs = new List<StoreStaff>
            {
                new StoreStaff { StoreId = anyCyberwaveStore.StoreId, UserId = staff1!.Id, IsPrimary = true },
                new StoreStaff { StoreId = anyPixelforgeStore.StoreId, UserId = staff2!.Id, IsPrimary = true },
            };

            await _context.StoreStaffs.AddRangeAsync(storeStaffs);
            await _context.SaveChangesAsync();
        }

        private async Task SeedStoreAccountsAsync()
        {
            if (await _context.Set<StoreAccount>().AnyAsync()) return;
            var stores = await _context.Stores.ToListAsync();

            var accounts = stores.Select((s, idx) => new StoreAccount
            {
                StoreId = s.StoreId,
                BankName = "VCB",
                AccountNumber = $"001100{1000 + idx}",
                AccountHolder = s.Name
            }).ToList();

            await _context.AddRangeAsync(accounts);
            await _context.SaveChangesAsync();
        }

        // ===================== 9) PRICING RULES =====================
        // Rule mẫu:
        // - 00:00–08:00: multiplier 0.8 (giờ thấp điểm)
        // - 18:00–22:00: multiplier 1.2 (giờ cao điểm)
        // - Weekend (Sat/Sun) 10:00–22:00: multiplier 1.15
        private async Task SeedPricingRulesAsync()
        {
            if (await _context.Set<PricingRule>().AnyAsync()) return;

            var stores = await _context.Stores.ToListAsync();
            var rules = new List<PricingRule>();

            foreach (var s in stores)
            {
                // thấp điểm cho toàn store
                rules.Add(new PricingRule
                {
                    StoreId = s.StoreId,
                    RoomType = null,              // áp dụng toàn store
                    StartHour = 0,
                    EndHour = 8,
                    HourlyMultiplier = 0.8m,
                    DayOfWeek = null,             // mọi ngày
                    Description = "Off-peak 00:00–08:00",
                    Status = PricingStatus.Active
                });

                // cao điểm tối cho toàn store
                rules.Add(new PricingRule
                {
                    StoreId = s.StoreId,
                    RoomType = null,
                    StartHour = 18,
                    EndHour = 22,
                    HourlyMultiplier = 1.2m,
                    DayOfWeek = null,
                    Description = "Peak 18:00–22:00",
                    Status = PricingStatus.Active
                });

                // weekend rule cho phòng VIP
                rules.Add(new PricingRule
                {
                    StoreId = s.StoreId,
                    RoomType = RoomType.vip,
                    StartHour = 10,
                    EndHour = 22,
                    HourlyMultiplier = 1.15m,
                    DayOfWeek = "Sat",
                    Description = "VIP weekend Sat",
                    Status = PricingStatus.Active
                });
                rules.Add(new PricingRule
                {
                    StoreId = s.StoreId,
                    RoomType = RoomType.vip,
                    StartHour = 10,
                    EndHour = 22,
                    HourlyMultiplier = 1.15m,
                    DayOfWeek = "Sun",
                    Description = "VIP weekend Sun",
                    Status = PricingStatus.Active
                });
            }

            await _context.AddRangeAsync(rules);
            await _context.SaveChangesAsync();
        }

        // ===================== 10) VOUCHERS =====================
        private async Task SeedVouchersAsync()
        {
            if (await _context.Vouchers.AnyAsync()) return;

            var store1 = await _context.Stores.FirstOrDefaultAsync();
            var store2 = await _context.Stores.Skip(1).FirstOrDefaultAsync();

            var vouchers = new List<Voucher>
            {
                new Voucher
                {
                    Code = "WELCOME10",
                    Description = "Giảm 10% cho khách hàng mới",
                    DiscountPercent = 10,
                    MaxDiscountAmount = 100000,     // trần giảm
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(3),
                    UsageLimit = 500,
                    UsedCount = 0,
                    Status = VoucherStatus.Active
                },
                new Voucher
                {
                    Code = "STORE50K",
                    Description = "Giảm 50.000đ cho đơn hàng tại Store A",
                    StoreId = store1?.StoreId,
                    DiscountAmount = 50000,
                    MinOrderAmount = 200000,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(2),
                    UsageLimit = 100,
                    Status = VoucherStatus.Active
                },
                new Voucher
                {
                    Code = "VIP20",
                    Description = "Voucher giảm 20% dành riêng cho thành viên VIP",
                    StoreId = store2?.StoreId,
                    DiscountPercent = 20,
                    MinOrderAmount = 100000,
                    MaxDiscountAmount = 100000,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    UsageLimit = 50,
                    Status = VoucherStatus.Active
                }
            };

            // tránh trùng Code nếu chạy nhiều lần / data tồn tại
            vouchers = vouchers
                .Where(v => !_context.Vouchers.Any(x => x.Code == v.Code))
                .ToList();

            if (vouchers.Count > 0)
            {
                _context.Vouchers.AddRange(vouchers);
                await _context.SaveChangesAsync();
            }
        }

        // ===================== 11) Booking demo (tuỳ chọn) =====================
        private async Task SeedSampleBookingsAsync()
        {
            if (await _context.Bookings.AnyAsync()) return;

            var store = await _context.Stores.Include(s => s.Floors).FirstAsync();
            var user = await FindUserAsync("user1");
            if (user == null) return;

            var anyRoom = await _context.Rooms
                .Include(r => r.Floor).ThenInclude(f => f.Store)
                .Where(r => r.Floor.StoreId == store.StoreId)
                .FirstOrDefaultAsync();

            if (anyRoom == null) return;

            var machines = await _context.Machines
                .Where(m => m.RoomId == anyRoom.RoomId)
                .OrderBy(m => m.MachineId)
                .Take(2)
                .ToListAsync();

            var start = DateTime.UtcNow.AddDays(1).Date.AddHours(14); // 14:00 ngày mai (UTC)
            var end = start.AddHours(2);

            var booking = new Booking
            {
                StoreId = store.StoreId,
                ClientId = user.Id,
                BookingCode = $"BK-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                StartAt = start,
                EndAt = end,
                Status = BookingStatus.reserved,
                EstimatedAmt = (anyRoom.HourlyRate * 2) * machines.Count,
                Note = "Demo booking 2 máy"
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            // gán 2 máy
            var bookingMachines = machines.Select((m, i) => new BookingMachine
            {
                BookingId = booking.BookingId,
                MachineId = m.MachineId,
                RateSnapshot = anyRoom.HourlyRate
            }).ToList();

            await _context.AddRangeAsync(bookingMachines);
            await _context.SaveChangesAsync();
        }
    }
}
