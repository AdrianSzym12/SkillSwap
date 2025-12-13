using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;


namespace SkillSwap.Application.Mappers
{
    public class ProfileMapper : AutoMapper.Profile
    {
        public ProfileMapper()
        {
            CreateMap<Domain.Entities.Database.Profile, ProfileDTO>()
                .ForMember(d => d.id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.User, opt => opt.MapFrom(s => s.User))
                .ReverseMap()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.id))
                .ForMember(d => d.User, opt => opt.Ignore()); 
        }
    }

}
