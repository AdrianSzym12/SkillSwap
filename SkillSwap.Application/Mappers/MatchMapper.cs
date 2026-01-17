using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class MatchMapper : AutoMapper.Profile
    {
        public MatchMapper()
        {
            CreateMap<Match, MatchDTO>()
                .ForMember(dest => dest.profile1, opt => opt.Ignore())
                .ForMember(dest => dest.profile2, opt => opt.Ignore());
        }
    }
}
