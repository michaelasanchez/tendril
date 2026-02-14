
using AutoMapper;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;


namespace Tendril.Api.Mapping;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<ScraperAttemptHistory, AttemptHistoryDto>();

        CreateMap<ScrapedEventRaw, ScrapedEventRawDto>();

        CreateMap<ScraperDefinition, ScraperDto>()
            .ForMember(d => d.State, opt => opt.MapFrom(s => s.State.ToString()))
            .ForMember(d => d.LastSuccessUtc, opt => opt.MapFrom(s => s.LastSuccessUtc.HasValue ? s.LastSuccessUtc.Value.ToString("o") : null))
            .ForMember(d => d.LastFailureUtc, opt => opt.MapFrom(s => s.LastFailureUtc.HasValue ? s.LastFailureUtc.Value.ToString("o") : null));

        CreateMap<Event, EventDto>()
            .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category!.Name))
            .ForMember(d => d.VenueName, opt => opt.MapFrom(s => s.Venue!.Name))
            .ForMember(d => d.VenueUrl, opt => opt.MapFrom(s => s.Venue!.Website))
            .ForMember(d => d.UpdatedUtc, opt => opt.MapFrom(s => s.UpdatedAtUtc));

        CreateMap<Category, CategoryDto>();

        CreateMap<Tag, TagDto>();

        CreateMap<Venue, VenueDto>();
    }
}
