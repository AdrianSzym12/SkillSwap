using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class KanbanBoardMapper : AutoMapper.Profile
    {
        public KanbanBoardMapper()
        {
            CreateMap<KanbanBoard, KanbanBoardDTO>().ReverseMap();

        }
    }
}
