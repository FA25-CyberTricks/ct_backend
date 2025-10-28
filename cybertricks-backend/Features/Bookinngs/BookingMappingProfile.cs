using ct.backend.Domain.Entities;

namespace ct.backend.Features.Bookinngs
{
    public class BookingMappingProfile
        : AbstractMappingProfile<Booking, BookingDto, CreateBookingRequest, UpdateBookingRequest>
    {
        public BookingMappingProfile()
        {
            CreateMap<Booking, BookingDto>()
                // nếu bạn giữ tên cũ là bookingMachineDtos thì sửa dòng dưới cho khớp tên
                .ForMember(d => d.BookingMachines,
                    opt => opt.MapFrom(s => s.BookingMachines))
                // nếu Client ở Booking là User (hoặc tên khác), map rõ ràng:
                .ForMember(d => d.Client,
                    opt => opt.MapFrom(s => s.Client)) // đổi s.Client nếu property tên khác
                ;

            // map con → dto con (có lấy Machine.Code)
            CreateMap<User, ClientDto>();

            CreateMap<BookingMachine, BookingMachineDto>()
                .ForMember(d => d.MachineCode,
                    opt => opt.MapFrom(s => s.Machine.Code));
        }
    }
}
