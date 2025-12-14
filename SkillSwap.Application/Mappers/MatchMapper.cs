using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class MatchMapper : AutoMapper.Profile
    {
        public MatchMapper()
        {
            CreateMap<MatchDTO, Match>()
                .ForMember(dest => dest.Profile1Id, opt => opt.MapFrom(src => src.profile1.id))
                .ForMember(dest => dest.Profile2Id, opt => opt.MapFrom(src => src.profile2.id))
                .ForMember(dest => dest.Profile1, opt => opt.MapFrom(src => src.profile1))
                .ForMember(dest => dest.Profile2, opt => opt.MapFrom(src => src.profile2))
                .ReverseMap();
        }
    }
}
