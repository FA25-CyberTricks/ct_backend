using ct.backend.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Domain.Entities
{
    public class Machine : BaseEntity
    {
        public int MachineId { get; set; }

        public int RoomId { get; set; }
        public virtual Room Room { get; set; } = default!;

        [MaxLength(50)] 
        public string Code { get; set; } = default!;
        //public MachineStatus Status { get; set; } = MachineStatus.available;
        public string? SpecJson { get; set; }     // JSON

        public virtual IEnumerable<BookingMachine> BookingMachines { get; set; }
    }
}
