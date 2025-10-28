using ct.backend.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Machines
{
    public class CreateMachineRequest : AbstractRequest
    {
        public int RoomId { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = default!;
        public string? SpecJson { get; set; }     // JSON
    }
}