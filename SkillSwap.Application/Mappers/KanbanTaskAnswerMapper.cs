using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public  class KanbanTaskAnswerMapper : AutoMapper.Profile
    {
        public KanbanTaskAnswerMapper()
        {
            CreateMap<KanbanTaskAnswer, KanbanTaskAnswerDTO>()
                .ForMember(dest => dest.kanbanTask, opt => opt.MapFrom(src => src.kanbanTask))
                .ForMember(dest => dest.profile, opt => opt.MapFrom(src => src.Profile));

            CreateMap<KanbanTaskAnswerDTO, KanbanTaskAnswer>()
               .ForMember(dest => dest.ProfileId, opt => opt.MapFrom(src => src.profile.id))
               .ForMember(dest => dest.kanbanTask, opt => opt.Ignore()) 
               .ForMember(dest => dest.Profile, opt => opt.Ignore())
               .ForMember(dest => dest.Checker, opt => opt.Ignore());
        }
    }
}
