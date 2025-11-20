
using AutoMapper;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;


namespace Tendril.Api.Mapping;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<ScraperDefinition, ScraperDto>()
            .ForMember(d => d.State, opt => opt.MapFrom(s => s.State.ToString()))
            .ForMember(d => d.LastSuccessUtc, opt => opt.MapFrom(s => s.LastSuccessUtc.HasValue ? s.LastSuccessUtc.Value.ToString("o") : null))
            .ForMember(d => d.LastFailureUtc, opt => opt.MapFrom(s => s.LastFailureUtc.HasValue ? s.LastFailureUtc.Value.ToString("o") : null));

        CreateMap<Venue, VenueDto>();

        CreateMap<Event, EventDto>()
            .ForMember(d => d.VenueName, opt => opt.MapFrom(s => s.Venue!.Name));
    }
}
