using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Bookinngs
{
    public class CreateBookingRequest : AbstractRequest
    {
        public int StoreId { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        [MinLength(1)]
        public List<int> MachineIds { get; init; } = new();

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}