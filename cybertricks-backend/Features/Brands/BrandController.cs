using AutoMapper;
using AutoMapper.QueryableExtensions;
using ct.backend.Common.Constants;
using ct.backend.Common.Error;
using ct.backend.Common.Pagination;
using ct.backend.Common.Validate;
using ct.backend.Domain.Entities;
using ct.backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ct.backend.Features.Brands
{
    [Route("api/[controller]")]
    [ApiController]
    public class BrandController : AbstractController<int, CreateBrandRequest, UpdateBrandRequest, QueryBrandRequest, BrandDto>
    {
        private readonly BookingDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BrandController> _logger;

        public BrandController(BookingDbContext context, IMapper mapper, ILogger<BrandController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Create a brand
        /// </summary>
        [Authorize(Roles = "Admin")]
        public override async Task<ActionResult<AbstractResponse<BrandDto>>> Create([FromBody] CreateBrandRequest request, CancellationToken ct)
        {
            var response = new BrandResponse<BrandDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }
            // Additional validations
            ErrorChecker.Email(response, request.ContactEmail, nameof(request.ContactEmail));
            ErrorChecker.Phone(response, request.ContactPhone, nameof(request.ContactPhone));
            if (!response.Success) return BadRequest(response);

            // 2. Check exist
            var exists = await _context.Brands.AnyAsync(b => b.Code == request.Code, ct);
            if (exists)
            {
                response.AddError(MessageCodes.E007, "Code already exists", nameof(request.Code));
                return Conflict(response);
            }

            // 3. Map request -> entity
            var brand = _mapper.Map<Brand>(request);

            try
            {
                // 4. Save to DB
                await _context.Brands.AddAsync(brand, ct);
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<BrandDto>(brand);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Create brand failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        /// Update a brand
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BrandDto>>> Update([FromRoute] int id, [FromBody] UpdateBrandRequest request, CancellationToken ct)
        {
            var response = new BrandResponse<BrandDto>();

            // 1. Validate request
            if (!ModelState.IsValid)
            {
                response.AddError(MessageCodes.E001);
                return BadRequest(response);
            }
            // Additional validations
            if (!string.IsNullOrWhiteSpace(request.ContactEmail))
                ErrorChecker.Email(response, request.ContactEmail, nameof(request.ContactEmail));
            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
                ErrorChecker.Phone(response, request.ContactPhone, nameof(request.ContactPhone));
            if (!response.Success) return BadRequest(response);

            // 2. Check exist
            var brand = await _context.Brands.FindAsync(id, ct);
            if (brand == null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            // Check duplicate code if code is provided in the request
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var exists = await _context.Brands
                    .AnyAsync(b => b.Code == request.Code && b.BrandId != id, ct);

                if (exists)
                {
                    response.AddError(MessageCodes.E007, nameof(request.Code)); // duplicate
                    return Conflict(response);
                }
            }

            // 3. Map request -> entity
            _mapper.Map(request, brand);
            brand.UpdatedAt = DateTime.UtcNow;

            try
            {
                // 4. Save to DB
                var rows = await _context.SaveChangesAsync(ct);

                // 5. Map entity -> dto
                var dto = _mapper.Map<BrandDto>(brand);

                // 6. Wrap response
                response.Data = dto;
                response.Message = MessageCodes.E000;
                return Ok(response);
            }
            catch (DbUpdateException)
            {
                response.AddError(MessageCodes.E999, "Update brand failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        /// <summary>
        ///  Delete a brand
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<object?>>> Delete([FromRoute] int id, CancellationToken ct)
        {
            var response = new BrandResponse<BrandDto>();

            var brand = await _context.Brands.FindAsync(id, ct);
            if (brand is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            _context.Brands.Remove(brand);
            var rows = await _context.SaveChangesAsync(ct);

            if (rows <= 0)
            {
                response.AddError(MessageCodes.E999, "Delete brand failed");
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }

            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get brand by id  
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<BrandDto>>> GetById([FromRoute] int id, CancellationToken ct)
        {
            var response = new BrandResponse<BrandDto>();

            var brand = await _context.Brands.FindAsync(id, ct);
            if (brand is null)
            {
                response.AddError(MessageCodes.E005, nameof(id));
                return NotFound(response);
            }

            var dto = _mapper.Map<BrandDto>(brand);

            response.Data = dto;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get all brands (no paging) - use with caution
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<IEnumerable<BrandDto>>>> GetAll(CancellationToken ct)
        {
            var response = new BrandResponse<IEnumerable<BrandDto>>();
            var brandDtos = await _context.Brands
                .AsNoTracking()
                .ProjectTo<BrandDto>(_mapper.ConfigurationProvider)
                .ToListAsync(ct);

            response.Data = brandDtos;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }

        /// <summary>
        /// Get brands with paging, filtering, sorting 
        /// </summary>
        public override async Task<ActionResult<AbstractResponse<PaginatedList<BrandDto>>>> GetPaged([FromQuery] QueryBrandRequest request, CancellationToken ct)
        {
            var response = new BrandResponse<PaginatedList<BrandDto>>();

            var query = _context.Brands.AsNoTracking();

            query = request.Desc
                ? query.OrderByDescending(b => b.Code)
                : query.OrderBy(b => b.Code);

            var projected = query.ProjectTo<BrandDto>(_mapper.ConfigurationProvider);

            var paged = await PaginatedList<BrandDto>.CreateAsync(
                            projected,
                            request.PageIndex,
                            request.PageSize,
                            ct);

            response.Data = paged;
            response.Message = MessageCodes.E000;
            return Ok(response);
        }
    }
}
