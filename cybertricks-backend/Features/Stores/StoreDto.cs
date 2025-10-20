using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Stores
{
    public class StoreDto
    {
        [Key]
        public int StoreId { get; set; }

        public int? BrandId { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; } = default!;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public decimal? Latitude { get; set; }   // DECIMAL(9,6) 

        public string? Avatar { get; set; }

        public int? Visited { get; set; }
    }
}