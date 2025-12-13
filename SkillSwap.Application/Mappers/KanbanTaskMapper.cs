using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public  class KanbanTaskMapper : AutoMapper.Profile
    {
        public KanbanTaskMapper()
        {
            CreateMap<KanbanTask, KanbanTaskDTO>()
                .ForMember(dest => dest.kanbanBoard, opt => opt.MapFrom(src => src.Board));

            CreateMap<KanbanTaskDTO, KanbanTask>()
                .ForMember(dest => dest.BoardId, opt => opt.MapFrom(src => src.kanbanBoard.Id))
                .ForMember(dest => dest.Board, opt => opt.Ignore()) 
                .ForMember(dest => dest.Assigned, opt => opt.Ignore());
        }
    }
}
