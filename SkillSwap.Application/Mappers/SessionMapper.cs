using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class SessionMapper : AutoMapper.Profile
    {
        public SessionMapper()
        {
            CreateMap<Session, SessionDTO>().ReverseMap();
        }
    }
}
