using AutoMapper;
using AutoMapper.QueryableExtensions;
using ct.backend.Common.Constants;
using ct.backend.Common.Error;
using ct.backend.Common.Pagination;
using ct.backend.Common.Validate;
using ct.backend.Domain.Entities;
using ct.backend.Features.Rooms;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ct.backend.Features.Stores
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoreController : AbstractController<int, CreateStoreRequest, UpdateStoreRequest, QueryStoreRequest, StoreDto>
    {
        private readonly BookingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StoreController> _logger;

        public StoreController(BookingDbContext context, IMapper mapper, ILogger<StoreController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Create a store
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<StoreDto>>> Create([FromBody] CreateStoreRequest request, CancellationToken ct)
        {
            var response = new StoreResponse<StoreDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }
            // Additional validations
            ErrorChecker.Phone(response, request.ContactPhone, nameof(request.ContactPhone));
            if (!response.Success) return BadRequest(response);

            // 2. Check exist
            var exists = await _context.Stores.AnyAsync(b =>
                b.BrandId == request.BrandId &&
                b.Name == request.Name, ct);

            if (exists)
            {
                response.AddError(MessageCodes.E007, "Name already exists", nameof(request.Name));
                return Conflict(response);
            }

            // 3. Map request -> entity
            var store = _mapper.Map<Store>(request);

            try
            {
                // 4. Save to DB
                await _context.Stores.AddAsync(store, ct);
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<StoreDto>(store);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Create store failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Update a store
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<StoreDto>>> Update([FromRoute] int id, [FromBody] UpdateStoreRequest request, CancellationToken ct)
        {
            var response = new StoreResponse<StoreDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }
            // Additional validations
            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
                ErrorChecker.Phone(response, request.ContactPhone, nameof(request.ContactPhone));
            if (!response.Success) return BadRequest(response);

            // 2. Check exist
            var store = await _context.Stores.FindAsync(id, ct);
            if (store == null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            // Check duplicate code if code is provided in the request
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var exists = await _context.Stores.AnyAsync(b =>
                    b.BrandId == request.BrandId &&
                    b.Name == request.Name &&
                    b.StoreId != id, ct);

                if (exists)
                {
                    response.AddError(MessageCodes.E007, nameof(request.Name)); // duplicate
                    return Conflict(response);
                }
            }

            // 3. Map request -> entity
            _mapper.Map(request, store);
            store.UpdatedAt = DateTime.UtcNow;

            try
            {
                // 4. Save to DB
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<StoreDto>(store);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Update store failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        ///  Delete a store
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<object?>>> Delete([FromRoute] int id, CancellationToken ct)
        {
            var response = new StoreResponse<StoreDto>();

            var store = await _context.Stores.FindAsync(id, ct);
            if (store is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            _context.Stores.Remove(store);
            var rows = await _context.SaveChangesAsync(ct);

            if (rows <= 0)
            {
                response.AddError(MessageCodes.E999, "Delete store failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get store by id  
        /// </summary>
        [HttpGet("{id}")]
        public override async Task<ActionResult<AbstractResponse<StoreDto>>> GetById([FromRoute] int id, CancellationToken ct)
        {
            var response = new StoreResponse<StoreDto>();

            var store = await _context.Stores.FindAsync(id, ct);
            if (store is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            var dto = _mapper.Map<StoreDto>(store);

            response.Data = dto;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get all stores (no paging) - use with caution
        /// </summary>
        [HttpGet("all")]
        public override async Task<ActionResult<AbstractResponse<IEnumerable<StoreDto>>>> GetAll(CancellationToken ct)
        {
            var response = new StoreResponse<IEnumerable<StoreDto>>();
            var storeDtos = await _context.Stores
                .AsNoTracking()
                .OrderBy(b => b.Latitude) // default order by newest
                .ProjectTo<StoreDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            response.Data = storeDtos;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Not implemented: Get stores with paging, filtering, sorting
        /// </summary>
        [HttpGet]
        public override async Task<ActionResult<AbstractResponse<PaginatedList<StoreDto>>>> GetPaged(
        [FromQuery] QueryStoreRequest request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get stores by brand id
        /// </summary>
        [HttpGet("by-brand")]
        public async Task<ActionResult<AbstractResponse<IEnumerable<StoreDto>>>> GetByBrand([FromQuery]int brandId, CancellationToken ct)
        {
            var response = new RoomResponse<IEnumerable<StoreDto>>();
            var storeDtos = await _context.Stores
                .AsNoTracking()
                .Where(s => s.BrandId == brandId)
                .ProjectTo<StoreDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            response.Data = storeDtos;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }
    }
}