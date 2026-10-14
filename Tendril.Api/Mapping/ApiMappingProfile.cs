using AutoMapper;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Mapping;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<ApiParameter, ApiParameterDto>();

        CreateMap<ScraperAttemptHistory, AttemptHistoryDto>();

        CreateMap<ScrapedEventRaw, ScrapedEventRawDto>();

        CreateMap<ScraperDefinition, ScraperDto>()
            .ForMember(d => d.State, opt => opt.MapFrom(s => s.State.ToString()))
            .ForMember(d => d.LastSuccessUtc, opt => opt.MapFrom(s => s.LastSuccessUtc.HasValue ? s.LastSuccessUtc.Value.ToString("o") : null))
            .ForMember(d => d.LastFailureUtc, opt => opt.MapFrom(s => s.LastFailureUtc.HasValue ? s.LastFailureUtc.Value.ToString("o") : null))
            .ForMember(d => d.Parents, opt => opt.MapFrom(s => s.ParentSelectors.Select(x => new ParentScraperDto
            {
                Id = x.ScraperDefinitionId,
                Name = x.ScraperDefinition.Name
            })))
            .ForMember(d => d.PendingEvents, opt => opt.MapFrom(s => s.Events.Count(x => x.Status == EventStatus.Pending)));

        CreateMap<Event, EventDto>()
            .ForMember(d => d.CategoryId, opt => opt.MapFrom(s => s.Category!.Id))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category!.Name))
            .ForMember(d => d.VenueName, opt => opt.MapFrom(s => s.Venue!.Name))
            .ForMember(d => d.VenueUrl, opt => opt.MapFrom(s => s.Venue!.Website))
            .ForMember(d => d.StartDateTime, opt => opt.MapFrom(s => s.StartPrecision == DatePrecision.Minute ? s.StartUtc : (DateTimeOffset?)null))
            .ForMember(d => d.StartDate, opt => opt.MapFrom(s => s.StartPrecision == DatePrecision.Minute ? (DateOnly?)null : DateOnly.FromDateTime(s.StartUtc.DateTime)))
            .ForMember(d => d.ShowStartTime, opt => opt.MapFrom(s => s.StartPrecision == DatePrecision.Minute))
            .ForMember(d => d.EndDateTime, opt => opt.MapFrom(s => s.EndPrecision == DatePrecision.Minute ? s.EndUtc : null))
            .ForMember(d => d.EndDate, opt => opt.MapFrom(s =>
                s.EndPrecision == DatePrecision.Minute || s.EndUtc == null
                    ? (DateOnly?)null
                    : DateOnly.FromDateTime(s.EndUtc.Value.DateTime)))
            .ForMember(d => d.ShowEndTime, opt => opt.MapFrom(s => s.EndPrecision == DatePrecision.Minute));

        CreateMap<Category, CategoryDto>();

        CreateMap<Tag, TagDto>();

        CreateMap<Venue, VenueDto>();

        CreateMap<ScheduledTask, ScheduledTaskDto>();
    }
}
