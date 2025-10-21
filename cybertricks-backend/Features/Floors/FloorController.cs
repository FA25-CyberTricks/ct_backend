using AutoMapper;
using AutoMapper.QueryableExtensions;
using ct.backend.Common.Constants;
using ct.backend.Common.Error;
using ct.backend.Common.Pagination;
using ct.backend.Common.Validate;
using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ct.backend.Features.Floors
{
    [Route("api/[controller]")]
    [ApiController]
    public class FloorController : AbstractController<int, CreateFloorRequest, UpdateFloorRequest, QueryFloorRequest, FloorDto>
    {
        private readonly BookingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<FloorController> _logger;

        public FloorController(BookingDbContext context, IMapper mapper, ILogger<FloorController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Create a floor
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<FloorDto>>> Create([FromBody] CreateFloorRequest request, CancellationToken ct)
        {
            var response = new FloorResponse<FloorDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2. Check exist
            var exists = await _context.Floors.AnyAsync(b => 
                b.StoreId == request.StoreId &&
                b.FloorNumber == request.FloorNumber, ct);

            if (exists)
            {
                response.AddError(MessageCodes.E007, "Code already exists", nameof(request.FloorNumber));
                return Conflict(response);
            }

            // 3. Map request -> entity
            var floor = _mapper.Map<Floor>(request);

            try
            {
                // 4. Save to DB
                await _context.Floors.AddAsync(floor, ct);
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<FloorDto>(floor);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Create floor failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Update a floor
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<FloorDto>>> Update([FromRoute] int id, [FromBody] UpdateFloorRequest request, CancellationToken ct)
        {
            var response = new FloorResponse<FloorDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2. Check exist
            var floor = await _context.Floors.FindAsync(id, ct);
            if (floor == null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            // Check duplicate code if code is provided in the request
            if (request.FloorNumber != null)
            {
                var exists = await _context.Floors.AnyAsync(b =>
                    b.StoreId == request.StoreId &&             
                    b.FloorNumber == request.FloorNumber && 
                    b.FloorId != id, ct);

                if (exists)
                {
                    response.AddError(MessageCodes.E007, nameof(request.FloorNumber)); // duplicate
                    return Conflict(response);
                }
            }

            // 3. Map request -> entity
            _mapper.Map(request, floor);
            floor.UpdatedAt = DateTime.UtcNow;

            try
            {
                // 4. Save to DB
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<FloorDto>(floor);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Update floor failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        ///  Delete a floor
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<object?>>> Delete([FromRoute] int id, CancellationToken ct)
        {
            var response = new FloorResponse<FloorDto>();

            var floor = await _context.Floors.FindAsync(id, ct);
            if (floor is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            _context.Floors.Remove(floor);
            var rows = await _context.SaveChangesAsync(ct);

            if (rows <= 0)
            {
                response.AddError(MessageCodes.E999, "Delete floor failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get floor by id  
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<FloorDto>>> GetById([FromRoute] int id, CancellationToken ct)
        {
            var response = new FloorResponse<FloorDto>();

            var floor = await _context.Floors.FindAsync(id, ct);
            if (floor is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            var dto = _mapper.Map<FloorDto>(floor);

            response.Data = dto;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get all floors (no paging) - use with caution
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<IEnumerable<FloorDto>>>> GetAll(CancellationToken ct)
        {
            var response = new FloorResponse<IEnumerable<FloorDto>>();
            var floorDtos = await _context.Floors
                .AsNoTracking()
                .ProjectTo<FloorDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            response.Data = floorDtos;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }


        /// <summary>
        /// Get floors with paging, filtering, sorting 
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<PaginatedList<FloorDto>>>> GetPaged([FromQuery] QueryFloorRequest request, CancellationToken ct)
        {
            var response = new FloorResponse<PaginatedList<FloorDto>>();
            return Ok(response);
        }
    }
}
