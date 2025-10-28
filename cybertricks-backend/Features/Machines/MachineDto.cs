using ct.backend.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Machines
{
    public class MachineDto
    {
        [Key]
        public int MachineId { get; set; }

        public int? RoomId { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; } = default!;
        public MachineStatus? Status { get; set; }
        public string? SpecJson { get; set; }     // JSON

        public string? Label { get; set; }

        public int? RowIndex { get; set; }
        public int? ColIndex { get; set; }
    }
}