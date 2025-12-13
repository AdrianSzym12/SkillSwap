using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class UserSkillMapper : AutoMapper.Profile
    {
        public UserSkillMapper()
        {
            CreateMap<UserSkill, UserSkillDTO>()
           .ReverseMap();
        }
    }
}
