using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class SkillMapper : AutoMapper.Profile
    {
        public SkillMapper()
        {
            CreateMap<Skill, SkillDTO>()
                .ReverseMap();
        }
    }
}
