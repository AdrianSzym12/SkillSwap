using AutoMapper;
using SkillSwap.Application.DTO;
using SkillSwap.Domain.Entities.Database;
using Profile = AutoMapper.Profile;

namespace SkillSwap.Application.Mappers
{
    public class ReviewMapper : Profile
    {
        public ReviewMapper()
        {
            CreateMap<Review, ReviewDTO>().ReverseMap();
        }
    }
}
