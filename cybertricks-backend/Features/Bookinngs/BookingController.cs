using AutoMapper;
using AutoMapper.QueryableExtensions;
using ct.backend.Common.Constants;
using ct.backend.Common.Pagination;
using ct.backend.Common.Validate;
using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ct.backend.Features.Bookinngs
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : AbstractController<int, CreateBookingRequest, UpdateBookingRequest, QueryBookingRequest, BookingDto>
    {
        private readonly BookingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BookingController> _logger;

        public BookingController(BookingDbContext context, IMapper mapper, ILogger<BookingController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Create a booking
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BookingDto>>> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
        {
            var response = new BookingResponse<BookingDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2. Check exist
            var exists = await _context.Bookings.AnyAsync(b =>
                b.BookingCode == request.BookingCode, ct);

            if (exists)
            {
                response.AddError(MessageCodes.E007, "BookingCode already exists", nameof(request.BookingCode));
                return Conflict(response);
            }

            // 3. Map request -> entity
            var booking = _mapper.Map<Booking>(request);

            try
            {
                // 4. Save to DB
                await _context.Bookings.AddAsync(booking, ct);
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<BookingDto>(booking);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Create booking failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Update a booking
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BookingDto>>> Update([FromRoute] int id, [FromBody] UpdateBookingRequest request, CancellationToken ct)
        {
            var response = new BookingResponse<BookingDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2. Check exist
            var booking = await _context.Bookings.FindAsync(id, ct);
            if (booking == null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            // Check duplicate code if code is provided in the request
            if (request.BookingCode != null)
            {
                var exists = await _context.Bookings.AnyAsync(b =>
                    b.BookingCode == request.BookingCode &&
                    b.BookingId != id, ct);

                if (exists)
                {
                    response.AddError(MessageCodes.E007, nameof(request.BookingCode)); // duplicate
                    return Conflict(response);
                }
            }

            // 3. Map request -> entity
            _mapper.Map(request, booking);
            booking.UpdatedAt = DateTime.UtcNow;

            try
            {
                // 4. Save to DB
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<BookingDto>(booking);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Update booking failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        ///  Delete a booking
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<object?>>> Delete([FromRoute] int id, CancellationToken ct)
        {
            var response = new BookingResponse<BookingDto>();

            var booking = await _context.Bookings.FindAsync(id, ct);
            if (booking is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            _context.Bookings.Remove(booking);
            var rows = await _context.SaveChangesAsync(ct);

            if (rows <= 0)
            {
                response.AddError(MessageCodes.E999, "Delete booking failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            response.Message = MessageCodes.E000;
            return Ok(response);
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
            var response = new BookingResponse<PaginatedList<BookingDto>>();
            return Ok(response);
        }
    }

}
