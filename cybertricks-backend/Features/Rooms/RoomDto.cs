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

        // 🔹 Layout metadata để FE vẽ sơ đồ
        public int GridX { get; set; }      // cột bắt đầu (1-based)
        public int GridY { get; set; }      // hàng bắt đầu
        public int GridW { get; set; }      // số cột chiếm
        public int GridH { get; set; }      // số hàng chiếm
        public int GridRows { get; set; }   // lưới máy bên trong: số hàng
        public int GridCols { get; set; }   // lưới máy bên trong: số cột
        [MaxLength(10)]
        public string? ColorHex { get; set; }
    }
}