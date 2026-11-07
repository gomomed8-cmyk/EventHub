using AutoMapper;
using EventHub.Application.DTOs.Booking;
using EventHub.Domain.Entities;


namespace EventHub.Application.Mappings
{
    public class BookingProfile : Profile
    {
        public BookingProfile()
        {

            CreateMap<CreateBookingItemRequest, BookingItem>();

            CreateMap<Booking, CreateBookingResponse>()
              .ForMember(dest => dest.BookingId,
                  opt => opt.MapFrom(src => src.Id))
              .ForMember(dest => dest.EventName,
                  opt => opt.MapFrom(src => src.Event.Name))
              .ForMember(dest => dest.Items,
                  opt => opt.MapFrom(src => src.BookingItems));


            CreateMap<BookingItem, CreateBookingItemResponse>()
                .ForMember(
                    dest => dest.CategoryName,
                    opt => opt.MapFrom(src =>
                        src.TicketCategory != null
                            ? src.TicketCategory.Name
                            : "General Admission"));
        }
    }
}
