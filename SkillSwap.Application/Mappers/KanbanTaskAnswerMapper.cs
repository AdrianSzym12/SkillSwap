using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class KanbanTaskAnswerMapper : AutoMapper.Profile
    {
        public KanbanTaskAnswerMapper()
        {
            CreateMap<KanbanTaskAnswer, KanbanTaskAnswerDTO>()
                .ReverseMap();
        }
    }
}
