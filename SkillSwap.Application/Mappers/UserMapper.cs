using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;

namespace SkillSwap.Application.Mappers
{
    public class UserMapper : AutoMapper.Profile
    {
        public UserMapper()
        {
            CreateMap<User, UserDTO>()
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.PasswordHash))
                .ReverseMap();
        }
    }
}
