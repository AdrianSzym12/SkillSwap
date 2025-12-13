using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class KanbanBoardMapper : AutoMapper.Profile
    {
        public KanbanBoardMapper()
        {
            CreateMap<KanbanBoard, KanbanBoardDTO>()
                .ForMember(dest => dest.user, opt => opt.Ignore()) 
                .ForMember(dest => dest.match, opt => opt.Ignore());

            CreateMap<KanbanBoardDTO, KanbanBoard>()
                .ForMember(dest => dest.MatchId, opt => opt.MapFrom(src => src.match.Id))
                .ForMember(dest => dest.AuthorId, opt => opt.MapFrom(src => src.user.id))
                .ForMember(dest => dest.Match, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore());
        }
    }
}
