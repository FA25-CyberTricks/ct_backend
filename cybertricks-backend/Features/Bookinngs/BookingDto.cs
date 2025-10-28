using ct.backend.Domain.Entities;
using ct.backend.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace ct.backend.Features.Bookinngs
{
    public class BookingDto
    {
        [Key]
        public int BookingId { get; set; }
        public int? StoreId { get; set; }
        public string? ClientId { get; set; }
        public ClientDto Client { get; set; }

        [MaxLength(30)]
        public string? BookingCode { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public BookingStatus? Status { get; set; }
        public decimal? EstimatedAmt { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public IEnumerable<BookingMachineDto> BookingMachines { get; set; } = new List<BookingMachineDto>();
    }

    public class ClientDto
    {
        public string FullName { get; set; }
    }

    public class BookingMachineDto
    {

        public string MachineCode { get; set; }

        public decimal? RateSnapshot { get; set; }
    }
}