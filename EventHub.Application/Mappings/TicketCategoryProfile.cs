using AutoMapper;
using EventHub.Application.DTOs.Events;
using EventHub.Domain.Entities;


namespace EventHub.Application.Mappings
{
    internal class TicketCategoryProfile : Profile
    {
        public TicketCategoryProfile()
        {
            
            CreateMap<TicketCategoryRequest, TicketCategory>();

            CreateMap<TicketCategory, TicketCategoryResponse>()
          .ForMember(
          dest => dest.CategoryId,
          opt => opt.MapFrom(src => src.Id));
        }
    }
}
