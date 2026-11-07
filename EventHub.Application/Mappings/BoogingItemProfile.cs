using AutoMapper;
using EventHub.Application.DTOs.Booking;
using EventHub.Domain.Entities;


namespace EventHub.Application.Mappings
{
    public class BoogingItemProfile : Profile
    {
        public BoogingItemProfile()
        {
            CreateMap<CreateBookingItemRequest, BookingItem>();
        }
    }
}
