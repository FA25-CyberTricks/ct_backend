using ct.backend.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Domain.Entities
{
    public class Room : BaseEntity
    {
        public int RoomId { get; set; }

        public int FloorId { get; set; }
        public virtual Floor Floor { get; set; } = default!;
        public decimal HourlyRate { get; set; }

        [MaxLength(200)] 
        public string Name { get; set; } = default!;

        public RoomType? Type { get; set; }
        public int? Capacity { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.active;
        public int DisplayOrder { get; set; } = 0;

        public virtual IEnumerable<Machine> Machines { get; set; }
    }
}
