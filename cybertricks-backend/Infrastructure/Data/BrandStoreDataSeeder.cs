using ct.backend.Domain.Entities;
using ct.backend.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ct.backend.Infrastructure.Data
{
    public sealed class BrandStoreDataSeeder
    {
        private readonly BookingDbContext _db;
        private readonly ILogger<BrandStoreDataSeeder> _logger;

        public BrandStoreDataSeeder(BookingDbContext db, ILogger<BrandStoreDataSeeder> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> SeedAllAsync()
        {
            var r1 = await SeedBrandAsync();
            var r2 = await SeedBOOMMenuAsync(
                brandId: (await _db.Brands.FirstAsync(b => b.Code == "BOOM_CYBER_GAME")).BrandId);

            var r3 = await Seed36MenuAsync(
                brandId: (await _db.Brands.FirstAsync(b => b.Code == "36_CYBER_GAMING")).BrandId);

            var r4 = await SeedBOOMMachine("BOOM_CYBER_GAME");
            var r5 = await Seed36MachineAsync("36_CYBER_GAMING");

            return true;
        }
        private async Task<int> SeedBrandAsync(CancellationToken ct = default)
        {
            // ====== DATA TỪ HÌNH BẠN GỬI ======
            var brands = new List<Brand>
            {
                new Brand
                {
                    Code = "KING_GAMING",
                    Name = "King Gaming",
                    Status = BrandStatus.active,
                    AvgRating = 4,
                    RatingCount = 18,
                    Stores = new List<Store>
                    {
                        new Store
                        {
                            Name = "King Gaming",
                            Address = "214 Châu Thị Vĩnh Tế, Bắc Mỹ An, Ngũ Hành Sơn, Đà Nẵng 50500",
                            ContactPhone = "0947 213 555",
                            Description = "Quán cà phê internet • Rating 3.5 (8)",
                            DisplayOrder = 1,
                            Status = StoreStatus.active,
                            Avatar = "",
                            CoverImage = "",
                            Visited = 0,
                            Latitude = 16.046838m,
                            Longitude = 108.2413778m
                        },
                        new Store
                        {
                            Name = "King Gaming 2",
                            Address = "504 Trần Đại Nghĩa, Hoà Hải, Ngũ Hành Sơn, Đà Nẵng 550000",
                            ContactPhone = "0935 925 325",
                            Description = "Cửa hàng internet • Rating 4.5 (10)",
                            DisplayOrder = 2,
                            Status = StoreStatus.active,
                            Avatar = "",
                            CoverImage = "",
                            Visited = 0,
                            Latitude = 15.9732533m,
                            Longitude = 108.2544679m
                        }
                    }
                },
                new Brand
                {
                    Code = "FUN_STADIUM",
                    Name = "Fun Stadium",
                    Status = BrandStatus.active,
                    AvgRating = 3.3,
                    RatingCount = 16,
                    Stores = new List<Store>
                    {
                        new Store
                        {
                            Name = "Fun Stadium",
                            Address = "234 Nam Kỳ Khởi Nghĩa, Hoà Hải, Ngũ Hành Sơn, Đà Nẵng 550000",
                            ContactPhone = "0934 701 023",
                            Description = "Cửa hàng internet • Rating 3.3 (16)",
                            DisplayOrder = 1,
                            Status = StoreStatus.active,
                            Avatar = "",
                            CoverImage = "",
                            Visited = 0,
                            Latitude = 15.9782151m,
                            Longitude = 108.2510278m
                        }
                    }
                },
                new Brand
                {
                    Code = "INTERNET_PRO_GAMING",
                    Name = "INTERNET PRO GAMING",
                    Status = BrandStatus.active,
                    AvgRating = 5,
                    RatingCount = 4,
                    Stores = new List<Store>
                    {
                        new Store
                        {
                            Name = "INTERNET PRO GAMING",
                            Address = "Nam Kỳ Khởi Nghĩa, Ngũ Hành Sơn, Đà Nẵng 550000",
                            ContactPhone = "0702 068 841",
                            Description = "Nhà cung cấp dịch vụ Internet • Rating 5.0 (4)",
                            DisplayOrder = 1,
                            Status = StoreStatus.active,
                            Avatar = "",
                            CoverImage = "",
                            Visited = 0,
                            Latitude = 15.9780544m,
                            Longitude = 108.2529181m
                        }
                    }
                },
                new Brand
                {
                    Code = "36_CYBER_GAMING",
                    Name = "36 Cyber Gaming",
                    Status = BrandStatus.active,
                    AvgRating = 4.8,
                    RatingCount = 31,
                    Stores = new List<Store>
                    {
                        new Store
                        {
                            Name = "36 Cyber Gaming",
                            Address = "117 Nam Kỳ Khởi Nghĩa, Ngũ Hành Sơn, Đà Nẵng 50000",
                            ContactPhone = "0949 789 196",
                            Description = "Quán cà phê internet • Rating 4.8 (31)",
                            DisplayOrder = 1,
                            Status = StoreStatus.active,
                            Avatar = "",
                            CoverImage = "",
                            Visited = 0,
                            Latitude = 15.9778075m,
                            Longitude = 15.9778075m
                        }
                    }
                },
                new Brand
                {
                    Code = "BOOM_CYBER_GAME",
                    Name = "BOOM CYBER GAME",
                    Status = BrandStatus.active,
                    AvgRating = 3.8,
                    RatingCount = 25,
                    Stores = new List<Store>
                    {
                        new Store
                        {
                            Name = "BOOM CYBER GAME",
                            Address = "ĐT607/279 Trần Đại Nghĩa, Điện Ngọc, Ngũ Hành Sơn, Đà Nẵng 550000",
                            ContactPhone = "0934 701 023",
                            Description = "Cửa hàng trò chơi điện tử • Rating 3.8 (25)",
                            DisplayOrder = 1,
                            Status = StoreStatus.active,
                            Avatar = "",
                            CoverImage = "",
                            Visited = 0,
                            Latitude = 15.9751135m,
                            Longitude = 108.2550298m
                        }
                    }
                }
            };

            // ====== UPSERT ======
            int affected = 0;

            foreach (var b in brands)
            {
                // Upsert Brand theo Code
                var brand = await _db.Brands.FirstOrDefaultAsync(x => x.Code == b.Code, ct);
                if (brand is null)
                {
                    brand = new Brand
                    {
                        Code = b.Code,
                        Name = b.Name,
                        Status = BrandStatus.active,
                        AvgRating = b.AvgRating,
                        RatingCount = b.RatingCount
                    };
                    _db.Brands.Add(brand);
                    await _db.SaveChangesAsync(ct);
                    affected++;
                }
                else
                {
                    var changed = false;
                    if (brand.Name != b.Name) { brand.Name = b.Name; changed = true; }
                    if (brand.Status != BrandStatus.active) { brand.Status = BrandStatus.active; changed = true; }
                    if (changed)
                    {
                        _db.Brands.Update(brand);
                        await _db.SaveChangesAsync(ct);
                        affected++;
                    }
                }

                // Upsert Stores theo (BrandId + Name)
                foreach (var s in (b.Stores ?? Enumerable.Empty<Store>()))
                {
                    var store = await _db.Stores
                        .FirstOrDefaultAsync(x => x.BrandId == brand.BrandId && x.Name == s.Name, ct);

                    if (store is null)
                    {
                        store = new Store
                        {
                            BrandId = brand.BrandId,
                            Name = s.Name,
                            Address = s.Address,
                            ContactPhone = s.ContactPhone,
                            Description = s.Description,
                            DisplayOrder = s.DisplayOrder,
                            Status = StoreStatus.active,
                            Avatar = s.Avatar,
                            CoverImage = s.CoverImage,
                            Visited = s.Visited,
                            Latitude = s.Latitude,
                            Longitude = s.Longitude
                        };
                        _db.Stores.Add(store);
                        affected++;
                    }
                    else
                    {
                        var changed = false;
                        if (store.Address != s.Address) { store.Address = s.Address; changed = true; }
                        if (store.ContactPhone != s.ContactPhone) { store.ContactPhone = s.ContactPhone; changed = true; }
                        if (store.Description != s.Description) { store.Description = s.Description; changed = true; }
                        if (store.DisplayOrder != s.DisplayOrder) { store.DisplayOrder = s.DisplayOrder; changed = true; }
                        if (store.Status != StoreStatus.active) { store.Status = StoreStatus.active; changed = true; }

                        if (changed)
                        {
                            _db.Stores.Update(store);
                            affected++;
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }
            }

            _logger.LogInformation("BrandStoreDataSeeder done. Rows affected: {Affected}", affected);
            return affected;
        }

        private async Task<int> Seed36MachineAsync(string brandCode, CancellationToken ct = default)
        {
            int affected = 0;

            // 1) Brand + store đầu tiên
            var brand = await _db.Brands
                .Include(b => b.Stores)
                .FirstOrDefaultAsync(b => b.Code == brandCode, ct);

            if (brand is null || brand.Stores == null || !brand.Stores.Any())
            {
                _logger.LogWarning("Seed36MachineAsync: Brand '{BrandCode}' không tồn tại hoặc chưa có store.", brandCode);
                return 0;
            }

            var store = brand.Stores
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.StoreId)
                .First();

            // 2) Upsert Floor (tầng 1)
            var floor = await _db.Floors
                .FirstOrDefaultAsync(f => f.StoreId == store.StoreId && f.FloorNumber == 1, ct);

            if (floor is null)
            {
                floor = new Floor
                {
                    StoreId = store.StoreId,
                    FloorNumber = 1,
                    Name = "Tầng 1",
                    Status = FloorStatus.active,
                    DisplayOrder = 1
                };
                _db.Floors.Add(floor);
                await _db.SaveChangesAsync(ct);
                affected++;
            }

            // 3) Spec JSON
            string Spec(object o) => JsonSerializer.Serialize(o);

            var specThiDau = Spec(new
            {
                motherboard = "B760",
                cpu = "i5-14600KF",
                gpu = "RTX 5060",
                ram = "32GB",
                monitor = "ROG 380Hz",
                mouse = "G403 HERO",
                keyboard = "EK75 Rapid Trigger",
                headset = "DareU EH925",
                mousepad = "ARM NB",
                chair = "Gaming Korea"
            });

            var specMoba = Spec(new
            {
                motherboard = "H610",
                cpu = "i5-12400F",
                gpu = "RTX 3050",
                ram = "16GB",
                monitor = "LG Ultragear 165Hz",
                mouse = "G102",
                keyboard = "Fuhlen D",
                headset = "DareU 722X",
                mousepad = "ARM EDRA",
                chair = "Speed Sofa Quy"
            });

            var specFps1 = Spec(new
            {
                motherboard = "B760",
                cpu = "i5-13400F",
                gpu = "RTX 4060",
                ram = "32GB",
                monitor = "HKC 320Hz",
                mouse = "G403 HERO",
                keyboard = "EK75 Rapid Trigger",
                headset = "DareU EH925",
                mousepad = "ARM NB",
                chair = "Gaming Korea"
            });

            var specPro = Spec(new
            {
                motherboard = "H510",
                cpu = "i5-10400F",
                gpu = "RTX 3050",
                ram = "16GB",
                monitor = "LG Ultragear 165Hz",
                mouse = "G102",
                keyboard = "Fuhlen D",
                headset = "DareU 722X",
                mousepad = "ARM EDRA",
                chair = "Speed Sofa Quy"
            });

            var specVip = Spec(new
            {
                motherboard = "H610",
                cpu = "i5-12400F",
                gpu = "RTX 3060",
                ram = "32GB",
                monitor = "Samsung 240Hz",
                mouse = "G403 HERO",
                keyboard = "V750 PRO",
                headset = "DareU EH925",
                mousepad = "ARM EDRA",
                chair = "Speed Sofa Xoay"
            });

            // 4) Định nghĩa 5 phòng + layout
            // GridX/Y 1-based; GridW/H: kích thước khối phòng trên sơ đồ; GridRows/Cols: lưới máy bên trong
            var roomDefs = new[]
            {
                new { Name="THI ĐẤU", Hour=0m, Capacity=10, Display=1, Prefix="TD",  Spec=specThiDau, Color="#FF3B3033", GridX=1, GridY=1, GridW=6, GridH=2, GridRows=1, GridCols=3 },
                new { Name="MOBA",     Hour=0m, Capacity=14, Display=2, Prefix="MO",  Spec=specMoba,   Color="#FFCC0033", GridX=7, GridY=1, GridW=6, GridH=2, GridRows=1, GridCols=3 },
                new { Name="FPS 1",    Hour=0m, Capacity=14, Display=3, Prefix="FPS", Spec=specFps1,   Color="#34C75933", GridX=1, GridY=3, GridW=6, GridH=2, GridRows=1, GridCols=3 },
                new { Name="PRO",      Hour=0m, Capacity=21, Display=4, Prefix="PR",  Spec=specPro,    Color="#32ADE633", GridX=7, GridY=3, GridW=6, GridH=2, GridRows=1, GridCols=3 },
                new { Name="VIP",      Hour=0m, Capacity=14, Display=5, Prefix="VP",  Spec=specVip,    Color="#AF52DE33", GridX=13,GridY=2, GridW=6, GridH=2, GridRows=1, GridCols=3 },
            };

            foreach (var rd in roomDefs)
            {
                // Upsert Room theo (FloorId + Name)
                var room = await _db.Rooms
                    .FirstOrDefaultAsync(r => r.FloorId == floor.FloorId && r.Name == rd.Name, ct);

                if (room is null)
                {
                    room = new Room
                    {
                        FloorId = floor.FloorId,
                        Name = rd.Name,
                        HourlyRate = rd.Hour,
                        Capacity = rd.Capacity,
                        Status = RoomStatus.active,
                        DisplayOrder = rd.Display,

                        GridX = rd.GridX,
                        GridY = rd.GridY,
                        GridW = rd.GridW,
                        GridH = rd.GridH,
                        GridRows = rd.GridRows,
                        GridCols = rd.GridCols,
                        ColorHex = rd.Color
                    };
                    _db.Rooms.Add(room);
                    await _db.SaveChangesAsync(ct);
                    affected++;
                }
                else
                {
                    var changed = false;
                    if (room.HourlyRate != rd.Hour) { room.HourlyRate = rd.Hour; changed = true; }
                    if (room.Capacity != rd.Capacity) { room.Capacity = rd.Capacity; changed = true; }
                    if (room.DisplayOrder != rd.Display) { room.DisplayOrder = rd.Display; changed = true; }
                    if (room.Status != RoomStatus.active) { room.Status = RoomStatus.active; changed = true; }

                    if (room.GridX != rd.GridX) { room.GridX = rd.GridX; changed = true; }
                    if (room.GridY != rd.GridY) { room.GridY = rd.GridY; changed = true; }
                    if (room.GridW != rd.GridW) { room.GridW = rd.GridW; changed = true; }
                    if (room.GridH != rd.GridH) { room.GridH = rd.GridH; changed = true; }
                    if (room.GridRows != rd.GridRows) { room.GridRows = rd.GridRows; changed = true; }
                    if (room.GridCols != rd.GridCols) { room.GridCols = rd.GridCols; changed = true; }
                    if (room.ColorHex != rd.Color) { room.ColorHex = rd.Color; changed = true; }

                    if (changed)
                    {
                        _db.Rooms.Update(room);
                        await _db.SaveChangesAsync(ct);
                        affected++;
                    }
                }

                // 5) 3 máy/phòng — xếp theo lưới 1x3, RowIndex=1, ColIndex=1..3
                var machineDefs = new[]
                {
                    new { Code=$"{rd.Prefix}-01", Label="01", Row=1, Col=1, Spec=rd.Spec },
                    new { Code=$"{rd.Prefix}-02", Label="02", Row=1, Col=2, Spec=rd.Spec },
                    new { Code=$"{rd.Prefix}-03", Label="03", Row=1, Col=3, Spec=rd.Spec },
                };

                foreach (var md in machineDefs)
                {
                    var existing = await _db.Machines
                        .FirstOrDefaultAsync(m => m.RoomId == room.RoomId && m.Code == md.Code, ct);

                    if (existing is null)
                    {
                        var machine = new Machine
                        {
                            RoomId = room.RoomId,
                            Code = md.Code,
                            Label = md.Label,
                            RowIndex = md.Row,
                            ColIndex = md.Col,
                            SpecJson = md.Spec
                        };
                        _db.Machines.Add(machine);
                        affected++;
                    }
                    else
                    {
                        var mChanged = false;
                        if (existing.Label != md.Label) { existing.Label = md.Label; mChanged = true; }
                        if (existing.RowIndex != md.Row) { existing.RowIndex = md.Row; mChanged = true; }
                        if (existing.ColIndex != md.Col) { existing.ColIndex = md.Col; mChanged = true; }
                        if (existing.SpecJson != md.Spec) { existing.SpecJson = md.Spec; mChanged = true; }

                        if (mChanged)
                        {
                            _db.Machines.Update(existing);
                            affected++;
                        }
                    }
                }

                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Seed36MachineAsync for brand {BrandCode} done. Rows affected: {Affected}", brandCode, affected);
            return affected;
        }

        private async Task<int> SeedBOOMMachine(string brandCode, CancellationToken ct = default)
        {
            int affected = 0;

            // 1) Brand + store THỨ HAI (nếu có), fallback store đầu
            var brand = await _db.Brands
                .Include(b => b.Stores)
                .FirstOrDefaultAsync(b => b.Code == brandCode, ct);

            if (brand is null || brand.Stores == null || !brand.Stores.Any())
            {
                _logger.LogWarning("SeedBOOMMachine: Brand '{BrandCode}' không tồn tại hoặc chưa có store.", brandCode);
                return 0;
            }

            var orderedStores = brand.Stores
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.StoreId)
                .ToList();

            var store = (orderedStores.Count >= 2) ? orderedStores[1] : orderedStores.First();

            // 2) Upsert Floor (tầng 1) của store này
            var floor = await _db.Floors
                .FirstOrDefaultAsync(f => f.StoreId == store.StoreId && f.FloorNumber == 1, ct);

            if (floor is null)
            {
                floor = new Floor
                {
                    StoreId = store.StoreId,
                    FloorNumber = 1,
                    Name = "Tầng trệt",
                    Status = FloorStatus.active,
                    DisplayOrder = 1
                };
                _db.Floors.Add(floor);
                await _db.SaveChangesAsync(ct);
                affected++;
            }

            // 3) Spec JSON (tái dùng bộ cấu hình)
            string Spec(object o) => JsonSerializer.Serialize(o);

            var specMoba = Spec(new
            {
                motherboard = "H610",
                cpu = "i5-12400F",
                gpu = "RTX 3050",
                ram = "16GB",
                monitor = "LG Ultragear 165Hz",
                mouse = "G102",
                keyboard = "Fuhlen D",
                headset = "DareU 722X",
                mousepad = "ARM EDRA",
                chair = "Speed Sofa Quy"
            });

            var specPro = Spec(new
            {
                motherboard = "H510",
                cpu = "i5-10400F",
                gpu = "RTX 3050",
                ram = "16GB",
                monitor = "LG Ultragear 165Hz",
                mouse = "G102",
                keyboard = "Fuhlen D",
                headset = "DareU 722X",
                mousepad = "ARM EDRA",
                chair = "Speed Sofa Quy"
            });

            var specVip = Spec(new
            {
                motherboard = "H610",
                cpu = "i5-12400F",
                gpu = "RTX 3060",
                ram = "32GB",
                monitor = "Samsung 240Hz",
                mouse = "G403 HERO",
                keyboard = "V750 PRO",
                headset = "DareU EH925",
                mousepad = "ARM EDRA",
                chair = "Speed Sofa Xoay"
            });

            // 4) CHỈ 3 PHÒNG với LAYOUT KHÁC (Grid khác hẳn so với seed 3/5 phòng trước)
            //   - GridRows/Cols = 2x2 (bên trong), 3 máy sẽ chiếm (1,1), (1,2), (2,1)
            var roomDefs = new[]
            {
        new { Name="MOBA", Hour=0m, Capacity=12, Display=1, Prefix="MO",  Spec=specMoba, Color="#FFCC0033", GridX=2,  GridY=1, GridW=5, GridH=3, GridRows=2, GridCols=2 },
        new { Name="PRO",  Hour=0m, Capacity=16, Display=2, Prefix="PR",  Spec=specPro,  Color="#32ADE633", GridX=8,  GridY=2, GridW=6, GridH=3, GridRows=2, GridCols=2 },
        new { Name="VIP",  Hour=0m, Capacity=10, Display=3, Prefix="VP",  Spec=specVip,  Color="#AF52DE33", GridX=15, GridY=1, GridW=5, GridH=4, GridRows=2, GridCols=2 },
    };

            foreach (var rd in roomDefs)
            {
                // Upsert Room theo (FloorId + Name)
                var room = await _db.Rooms
                    .FirstOrDefaultAsync(r => r.FloorId == floor.FloorId && r.Name == rd.Name, ct);

                if (room is null)
                {
                    room = new Room
                    {
                        FloorId = floor.FloorId,
                        Name = rd.Name,
                        HourlyRate = rd.Hour,
                        Capacity = rd.Capacity,
                        Status = RoomStatus.active,
                        DisplayOrder = rd.Display,

                        GridX = rd.GridX,
                        GridY = rd.GridY,
                        GridW = rd.GridW,
                        GridH = rd.GridH,
                        GridRows = rd.GridRows,
                        GridCols = rd.GridCols,
                        ColorHex = rd.Color
                    };
                    _db.Rooms.Add(room);
                    await _db.SaveChangesAsync(ct);
                    affected++;
                }
                else
                {
                    var changed = false;
                    if (room.HourlyRate != rd.Hour) { room.HourlyRate = rd.Hour; changed = true; }
                    if (room.Capacity != rd.Capacity) { room.Capacity = rd.Capacity; changed = true; }
                    if (room.DisplayOrder != rd.Display) { room.DisplayOrder = rd.Display; changed = true; }
                    if (room.Status != RoomStatus.active) { room.Status = RoomStatus.active; changed = true; }

                    if (room.GridX != rd.GridX) { room.GridX = rd.GridX; changed = true; }
                    if (room.GridY != rd.GridY) { room.GridY = rd.GridY; changed = true; }
                    if (room.GridW != rd.GridW) { room.GridW = rd.GridW; changed = true; }
                    if (room.GridH != rd.GridH) { room.GridH = rd.GridH; changed = true; }
                    if (room.GridRows != rd.GridRows) { room.GridRows = rd.GridRows; changed = true; }
                    if (room.GridCols != rd.GridCols) { room.GridCols = rd.GridCols; changed = true; }
                    if (room.ColorHex != rd.Color) { room.ColorHex = rd.Color; changed = true; }

                    if (changed)
                    {
                        _db.Rooms.Update(room);
                        await _db.SaveChangesAsync(ct);
                        affected++;
                    }
                }

                // 5) 3 máy/phòng — bố trí khác: (1,1), (1,2), (2,1)
                var machineDefs = new[]
                {
            new { Code=$"{rd.Prefix}-01", Label="01", Row=1, Col=1, Spec=rd.Spec },
            new { Code=$"{rd.Prefix}-02", Label="02", Row=1, Col=2, Spec=rd.Spec },
            new { Code=$"{rd.Prefix}-03", Label="03", Row=2, Col=1, Spec=rd.Spec },
        };

                foreach (var md in machineDefs)
                {
                    var existing = await _db.Machines
                        .FirstOrDefaultAsync(m => m.RoomId == room.RoomId && m.Code == md.Code, ct);

                    if (existing is null)
                    {
                        var machine = new Machine
                        {
                            RoomId = room.RoomId,
                            Code = md.Code,
                            Label = md.Label,
                            RowIndex = md.Row,
                            ColIndex = md.Col,
                            SpecJson = md.Spec
                        };
                        _db.Machines.Add(machine);
                        affected++;
                    }
                    else
                    {
                        var mChanged = false;
                        if (existing.Label != md.Label) { existing.Label = md.Label; mChanged = true; }
                        if (existing.RowIndex != md.Row) { existing.RowIndex = md.Row; mChanged = true; }
                        if (existing.ColIndex != md.Col) { existing.ColIndex = md.Col; mChanged = true; }
                        if (existing.SpecJson != md.Spec) { existing.SpecJson = md.Spec; mChanged = true; }

                        if (mChanged)
                        {
                            _db.Machines.Update(existing);
                            affected++;
                        }
                    }
                }

                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("SeedBOOMMachine for brand {BrandCode} at storeId {StoreId} done. Rows affected: {Affected}",
                brandCode, store.StoreId, affected);

            return affected;
        }

        private async Task<int> SeedBOOMMenuAsync(int brandId, CancellationToken ct = default)
        {
            int affected = 0;

            // ========== DỮ LIỆU MẪU ==========
            var categories = new List<MenuCategory>
            {
                new MenuCategory
                {
                    BrandId = brandId,
                    Name = "Đồ ăn nhẹ",
                    Active = true,
                    Items = new List<MenuItem>
                    {
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ tôm trứng + xúc xích",
                            Price = 20000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ tôm xúc xích",
                            Price = 15000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ tôm trứng + bò",
                            Price = 27000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ tôm bò",
                            Price = 22000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ xào trứng",
                            Price = 20000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ xào trứng + xúc xích",
                            Price = 25000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ xào xúc xích",
                            Price = 20000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Cơm chiên trứng + xúc xích",
                            Price = 25000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Mỳ xào bò",
                            Price = 25000m,
                            Active = true
                        },
                        new MenuItem
                        {
                            BrandId = brandId,
                            Name = "Bánh thèo lèo",
                            Price = 8000m,
                            Active = true
                        }
                    }
                },
                new MenuCategory
                {
                    BrandId = brandId,
                    Name = "Đồ uống",
                    Active = true,
                    Items = new List<MenuItem>
                    {
                        new MenuItem { BrandId = brandId, Name = "Revive Chanh Muối",  Price = 12000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Revive Trắng",       Price = 12000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Mirinda Cam Chai",   Price = 12000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Trà Ô Long Chanh",   Price = 12000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Trà Tea+",           Price = 12000m, Active = true },

                        new MenuItem { BrandId = brandId, Name = "Sting Vàng Lon Cao", Price = 13000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Sting Dâu Lon Cao",  Price = 13000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Twister Cam Ép",     Price = 13000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "7UP Chai TT",        Price =  7000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Twister Sữa Vị Cam", Price = 13000m, Active = true },
                    }
                }
            };

            // ========== UPSERT ==========

            foreach (var cat in categories)
            {
                // Kiểm tra Category theo (BrandId + Name)
                var existingCat = await _db.MenuCategories
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.BrandId == brandId && c.Name == cat.Name, ct);

                if (existingCat is null)
                {
                    _db.MenuCategories.Add(cat);
                    affected++;
                }
                else
                {
                    // update trạng thái nếu khác
                    if (existingCat.Active != cat.Active)
                    {
                        existingCat.Active = cat.Active;
                        _db.MenuCategories.Update(existingCat);
                        affected++;
                    }

                    // Seed từng MenuItem trong Category
                    foreach (var item in cat.Items ?? Enumerable.Empty<MenuItem>())
                    {
                        var existingItem = existingCat.Items
                            .FirstOrDefault(i => i.Name == item.Name);

                        if (existingItem is null)
                        {
                            item.CategoryId = existingCat.CategoryId;
                            _db.MenuItems.Add(item);
                            affected++;
                        }
                        else
                        {
                            var changed = false;
                            if (existingItem.Price != item.Price) { existingItem.Price = item.Price; changed = true; }
                            if (existingItem.Active != item.Active) { existingItem.Active = item.Active; changed = true; }
                            if (changed)
                            {
                                _db.MenuItems.Update(existingItem);
                                affected++;
                            }
                        }
                    }
                }

                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Menu seeding done for brand {BrandId}. Rows affected: {Affected}", brandId, affected);
            return affected;
        }

        private async Task<int> Seed36MenuAsync(int brandId, CancellationToken ct = default)
        {
            int affected = 0;

            // ========== DỮ LIỆU MẪU ==========
            var categories = new List<MenuCategory>
            {
                new MenuCategory
                {
                    BrandId = brandId,
                    Name = "Đồ uống",
                    Active = true,
                    Items = new List<MenuItem>
                    {
                        new MenuItem { BrandId = brandId, Name = "Cà phê sữa đá", Price = 25000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Trà đào cam sả", Price = 30000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Nước suối Aquafina", Price = 10000m, Active = true },
                    }
                },
                new MenuCategory
                {
                    BrandId = brandId,
                    Name = "Đồ ăn nhẹ",
                    Active = true,
                    Items = new List<MenuItem>
                    {
                        new MenuItem { BrandId = brandId, Name = "Mì xào bò", Price = 45000m, Active = true },
                        new MenuItem { BrandId = brandId, Name = "Bánh mì trứng", Price = 20000m, Active = true }
                    }
                }
            };

            // ========== UPSERT ==========

            foreach (var cat in categories)
            {
                // Kiểm tra Category theo (BrandId + Name)
                var existingCat = await _db.MenuCategories
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.BrandId == brandId && c.Name == cat.Name, ct);

                if (existingCat is null)
                {
                    _db.MenuCategories.Add(cat);
                    affected++;
                }
                else
                {
                    // update trạng thái nếu khác
                    if (existingCat.Active != cat.Active)
                    {
                        existingCat.Active = cat.Active;
                        _db.MenuCategories.Update(existingCat);
                        affected++;
                    }

                    // Seed từng MenuItem trong Category
                    foreach (var item in cat.Items ?? Enumerable.Empty<MenuItem>())
                    {
                        var existingItem = existingCat.Items
                            .FirstOrDefault(i => i.Name == item.Name);

                        if (existingItem is null)
                        {
                            item.CategoryId = existingCat.CategoryId;
                            _db.MenuItems.Add(item);
                            affected++;
                        }
                        else
                        {
                            var changed = false;
                            if (existingItem.Price != item.Price) { existingItem.Price = item.Price; changed = true; }
                            if (existingItem.Active != item.Active) { existingItem.Active = item.Active; changed = true; }
                            if (changed)
                            {
                                _db.MenuItems.Update(existingItem);
                                affected++;
                            }
                        }
                    }
                }

                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Menu seeding done for brand {BrandId}. Rows affected: {Affected}", brandId, affected);
            return affected;
        }
    }
}
