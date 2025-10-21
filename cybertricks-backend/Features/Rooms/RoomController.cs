using AutoMapper;
using AutoMapper.QueryableExtensions;
using ct.backend.Common.Constants;
using ct.backend.Common.Pagination;
using ct.backend.Common.Validate;
using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ct.backend.Features.Rooms
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : AbstractController<int, CreateRoomRequest, UpdateRoomRequest, QueryRoomRequest, RoomDto>
    {
        private readonly BookingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<RoomController> _logger;

        public RoomController(BookingDbContext context, IMapper mapper, ILogger<RoomController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Create a room
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<RoomDto>>> Create([FromBody] CreateRoomRequest request, CancellationToken ct)
        {
            var response = new RoomResponse<RoomDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2. Check exist
            var exists = await _context.Rooms.AnyAsync(b =>
                b.FloorId == request.FloorId &&
                b.Type == request.Type, ct);

            if (exists)
            {
                response.AddError(MessageCodes.E007, "Code already exists", nameof(request.Type));
                return Conflict(response);
            }

            // 3. Map request -> entity
            var room = _mapper.Map<Room>(request);

            try
            {
                // 4. Save to DB
                await _context.Rooms.AddAsync(room, ct);
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<RoomDto>(room);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Create room failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Update a room
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<RoomDto>>> Update([FromRoute] int id, [FromBody] UpdateRoomRequest request, CancellationToken ct)
        {
            var response = new RoomResponse<RoomDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }

            // 2. Check exist
            var room = await _context.Rooms.FindAsync(id, ct);
            if (room == null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            // Check duplicate code if code is provided in the request
            if (request.Type != null)
            {
                var exists = await _context.Rooms.AnyAsync(b =>
                    b.FloorId == request.FloorId &&
                    b.Type == request.Type &&
                    b.RoomId != id, ct);

                if (exists)
                {
                    response.AddError(MessageCodes.E007, nameof(request.Type)); // duplicate
                    return Conflict(response);
                }
            }

            // 3. Map request -> entity
            _mapper.Map(request, room);
            room.UpdatedAt = DateTime.UtcNow;

            try
            {
                // 4. Save to DB
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<RoomDto>(room);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Update room failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        ///  Delete a room
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<object?>>> Delete([FromRoute] int id, CancellationToken ct)
        {
            var response = new RoomResponse<RoomDto>();

            var room = await _context.Rooms.FindAsync(id, ct);
            if (room is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            _context.Rooms.Remove(room);
            var rows = await _context.SaveChangesAsync(ct);

            if (rows <= 0)
            {
                response.AddError(MessageCodes.E999, "Delete room failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get room by id  
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<RoomDto>>> GetById([FromRoute] int id, CancellationToken ct)
        {
            var response = new RoomResponse<RoomDto>();

            var room = await _context.Rooms.FindAsync(id, ct);
            if (room is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            var dto = _mapper.Map<RoomDto>(room);

            response.Data = dto;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get all rooms (no paging) - use with caution
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<IEnumerable<RoomDto>>>> GetAll(CancellationToken ct)
        {
            var response = new RoomResponse<IEnumerable<RoomDto>>();
            var roomDtos = await _context.Rooms
                .AsNoTracking()
                .ProjectTo<RoomDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            response.Data = roomDtos;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }


        /// <summary>
        /// Get rooms with paging, filtering, sorting 
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<PaginatedList<RoomDto>>>> GetPaged([FromQuery] QueryRoomRequest request, CancellationToken ct)
        {
            var response = new RoomResponse<PaginatedList<RoomDto>>();
            return Ok(response);
        }
    }
}
