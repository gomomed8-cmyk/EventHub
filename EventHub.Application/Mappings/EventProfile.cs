using AutoMapper;
using EventHub.Application.DTOs.Events;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;


namespace EventHub.Application.Mappings
{
    internal class EventProfile : Profile
    {
        public EventProfile()
        {
            CreateMap<CreateEventRequest, Event>();

            CreateMap<Event, EventResponse>()
                .ForMember(dest => dest.EventId,opt => opt.MapFrom(src => src.Id))
                .ForMember(
                    dest => dest.AvailableQuantity,opt => opt.MapFrom(src =>
                 src.TicketMode == TicketMode.Categorized
                ? src.TicketCategories.Sum(tc => tc.AvailableQuantity)
                : src.AvailableQuantity));

        }
    }
}
