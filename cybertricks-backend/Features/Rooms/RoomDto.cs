using ct.backend.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Rooms
{
    public class RoomDto
    {
        [Key]
        public int RoomId { get; set; }

        public int? FloorId { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; } = default!;

        public RoomType? Type { get; set; }
        public int? Capacity { get; set; }
    }
}