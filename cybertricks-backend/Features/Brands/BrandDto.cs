using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Brands
{
    public class BrandDto
    {
        [Key]
        public int BrandId { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; } = default!;

        [MaxLength(200)]
        public string? Name { get; set; } = default!;

        [MaxLength(255)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public string? Description { get; set; }
    }
}