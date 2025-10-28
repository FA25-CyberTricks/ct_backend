using ct.backend.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Floors
{
    public class FloorDto
    {
        [Key]
        public int FloorId { get; set; }

        public int? FloorNumber { get; set; }

        public int? StoreId { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; }
    }
}