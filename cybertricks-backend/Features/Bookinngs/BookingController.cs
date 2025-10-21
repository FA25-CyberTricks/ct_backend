using AutoMapper;
using AutoMapper.QueryableExtensions;
using ct.backend.Common.Constants;
using ct.backend.Common.Pagination;
using ct.backend.Common.Validate;
using ct.backend.Domain.Entities;
using ct.backend.Domain.Enum;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace ct.backend.Features.Bookinngs
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : AbstractController<int, CreateBookingRequest, UpdateBookingRequest, QueryBookingRequest, BookingDto>
    {
        private readonly BookingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BookingController> _logger;
        private readonly IHttpContextAccessor _http;

        public BookingController(BookingDbContext context, IMapper mapper, ILogger<BookingController> logger, IHttpContextAccessor http)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _http = http;
        }

        /// <summary>
        /// Create a booking
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BookingDto>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
        {
            var response = new BookingResponse<BookingDto>();

            // 1) Validate model
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2) Lấy userId từ JWT
            var userId = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                response.AddError(MessageCodes.E020, "User is not authenticated");
                return Unauthorized(response);
            }

            // 3) Validate thời gian & danh sách máy
            if (request.EndAt <= request.StartAt)
            {
                response.AddError(MessageCodes.E001, "EndAt must be after StartAt");
                return BadRequest(response);
            }

            if (request.MachineIds is null || request.MachineIds.Count == 0)
            {
                response.AddError(MessageCodes.E001, "At least one machine is required");
                return BadRequest(response);
            }

            // 4) Kiểm tra Store + Machines thuộc Store
            var storeExists = await _context.Stores
                .AsNoTracking()
                .AnyAsync(s => s.StoreId == request.StoreId, ct);

            if (!storeExists)
            {
                response.AddError(MessageCodes.E005, $"Store {request.StoreId} not found");
                return NotFound(response);
            }

            var machines = await _context.Machines
                .AsNoTracking()
                .Include(m => m.Room)
                .ThenInclude(r => r.Floor)
                .Where(m => request.MachineIds.Contains(m.MachineId) && m.Room.Floor.StoreId == request.StoreId)
                .ToListAsync(ct);

            if (machines.Count != request.MachineIds.Distinct().Count())
            {
                response.AddError(MessageCodes.E005, "Some MachineIds are invalid or not in this store");
                return BadRequest(response);
            }

            // 5) Check trùng lịch cơ bản (overlap)
            var hasOverlap = await _context.BookingMachines
                .Include(bm => bm.Booking)
                .AnyAsync(bm =>
                    request.MachineIds.Contains(bm.MachineId) &&
                    bm.Booking.StoreId == request.StoreId &&
                    bm.Booking.Status != BookingStatus.cancelled &&
                    // overlap [StartAt, EndAt)
                    bm.Booking.StartAt < request.EndAt &&
                    request.StartAt < bm.Booking.EndAt,
                    ct);

            if (hasOverlap)
            {
                response.AddError(MessageCodes.E008, "Some machines are already booked in the selected time range");
                return Conflict(response);
            }

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // 6) Tạo Booking (server set các field)
                var booking = new Booking
                {
                    StoreId = request.StoreId,
                    ClientId = userId,
                    BookingCode = GenerateBookingCode(request.StoreId),
                    StartAt = DateTime.SpecifyKind(request.StartAt, DateTimeKind.Utc), // lưu UTC
                    EndAt = DateTime.SpecifyKind(request.EndAt, DateTimeKind.Utc),
                    Status = BookingStatus.reserved,
                    Note = request.Note
                };

                // 7) Tính EstimatedAmt (snapshot giá)
                //    -> bạn có thể tùy biến logic trong CalcEstimatedAmountAsync theo PricingRule
                var rateSnapshots = await GetRateSnapshotsAsync(machines.Select(m => m.MachineId).ToList(), ct);
                var estimated = await CalcEstimatedAmountAsync(request.StoreId, request.StartAt, request.EndAt, rateSnapshots, ct);
                booking.EstimatedAmt = estimated;

                await _context.Bookings.AddAsync(booking, ct);
                await _context.SaveChangesAsync(ct); // để có BookingId

                // 8) Tạo BookingMachines (snapshot đơn giá theo máy)
                var bookingMachines = machines.Select(m => new BookingMachine
                {
                    BookingId = booking.BookingId,
                    MachineId = m.MachineId,
                    RateSnapshot = rateSnapshots.TryGetValue(m.MachineId, out var r) ? r : null
                }).ToList();

                await _context.BookingMachines.AddRangeAsync(bookingMachines, ct);
                await _context.SaveChangesAsync(ct);

                // 9) (Tuỳ hệ thống) Tạo Invoice trước rồi mới Payment
                //    Ở đây giả định có entity Invoice với các field cơ bản.
                var invoice = new Invoice
                {
                    // ví dụ các field: (tùy schema thực tế của bạn)
                    StoreId = booking.StoreId,
                    BookingId = booking.BookingId,
                    Subtotal = booking.EstimatedAmt ?? 0m,
                    Total = booking.EstimatedAmt ?? 0m,
                    Status = InvoiceStatus.open
                };
                await _context.Invoices.AddAsync(invoice, ct);
                await _context.SaveChangesAsync(ct);

                // 10) Tạo Payment (liên kết InvoiceId)
                var payment = new Payment
                {
                    InvoiceId = invoice.InvoiceId,
                    UserId = userId,
                    Method = PaymentMethod.cash,           // hoặc online, v.v.
                    Amount = invoice.Total,
                    Status = PaymentStatus.captured,       // hoặc authorized/pending tuỳ flow
                    ProviderRef = null,
                    PaidAt = DateTime.UtcNow
                };
                await _context.Payments.AddAsync(payment, ct);
                await _context.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);

                // 11) Map -> DTO & trả về
                var dto = _mapper.Map<BookingDto>(booking);
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync(ct);
                response.AddError(MessageCodes.E999, $"Create booking failed: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        private static string GenerateBookingCode(int storeId)
            => $"BK-{storeId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        /// <summary>
        /// Lấy snapshot đơn giá theo máy (ví dụ từ Machine.HourlyRate).
        /// Bạn chỉnh lại cho khớp schema thực tế: có thể lấy từ Machine, RoomType, v.v.
        /// </summary>
        private async Task<Dictionary<int, decimal?>> GetRateSnapshotsAsync(List<int> machineIds, CancellationToken ct)
        {
            // giả sử Machine có cột HourlyRate
            var items = await _context.Machines
                .AsNoTracking()
                .Include(m => m.Room)
                .Where(m => machineIds.Contains(m.MachineId))
                .Select(m => new { m.MachineId, m.Room.HourlyRate })
                .ToListAsync(ct);

            return items.ToDictionary(x => x.MachineId, x => (decimal?)x.HourlyRate);
        }

        /// <summary>
        /// Tính EstimatedAmt = (tổng đơn giá snapshot theo máy) * số giờ * các multiplier từ PricingRule (nếu có).
        /// Logic mẫu: multiplier mặc định = 1.0; merge các rule theo giờ và theo ngày.
        /// </summary>
        private async Task<decimal> CalcEstimatedAmountAsync(
            int storeId,
            DateTime startUtc,
            DateTime endUtc,
            Dictionary<int, decimal?> rateSnapshots,
            CancellationToken ct)
        {
            var hours = (decimal)(endUtc - startUtc).TotalHours;
            if (hours <= 0) return 0m;

            var baseSum = rateSnapshots.Values.Where(v => v.HasValue).Sum(v => v!.Value);

            // Lấy các rule active của store
            var rules = await _context.PricingRules
                .Where(r => r.StoreId == storeId && r.Status == PricingStatus.Active)
                .ToListAsync(ct);

            // Tính multiplier theo từng giờ (đơn giản hoá: lấy max multiplier khớp khung giờ/ngày)
            decimal multiplier = 1.0m;

            // ví dụ: áp dụng theo StartHour/EndHour & DayOfWeek (chuỗi "Saturday,Sunday")
            var local = startUtc; // nếu muốn theo giờ VN thì add +7
            var hour = local.Hour;
            var dow = local.DayOfWeek.ToString(); // "Saturday"

            foreach (var rule in rules)
            {
                var inHour = hour >= rule.StartHour && hour < rule.EndHour;
                var inDay = string.IsNullOrWhiteSpace(rule.DayOfWeek)
                             || rule.DayOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                              .Contains(dow, StringComparer.OrdinalIgnoreCase);

                if (inHour && inDay && rule.HourlyMultiplier.HasValue)
                    multiplier *= rule.HourlyMultiplier.Value;
            }

            return Math.Max(0, baseSum * hours * multiplier);
        }

        /// <summary>
        /// Update a booking
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BookingDto>>> Update([FromRoute] int id, [FromBody] UpdateBookingRequest request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Delete a booking
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<object?>>> Delete([FromRoute] int id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get booking by id  
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BookingDto>>> GetById([FromRoute] int id, CancellationToken ct)
        {
            var response = new BookingResponse<BookingDto>();

            var booking = await _context.Bookings.FindAsync(id, ct);
            if (booking is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            var dto = _mapper.Map<BookingDto>(booking);

            response.Data = dto;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get all bookings (no paging) - use with caution
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<IEnumerable<BookingDto>>>> GetAll(CancellationToken ct)
        {
            var response = new BookingResponse<IEnumerable<BookingDto>>();
            var bookingDtos = await _context.Bookings
                .AsNoTracking()
                .ProjectTo<BookingDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            response.Data = bookingDtos;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }


        /// <summary>
        /// Get bookings with paging, filtering, sorting 
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<PaginatedList<BookingDto>>>> GetPaged([FromQuery] QueryBookingRequest request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

}
