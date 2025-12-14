using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Application.Interfaces;
using SkillSwap.Application.Mappers;
using SkillSwap.Application.Services.Domain.Services;
using SkillSwap.Domain.Interfaces;

namespace SkillSwap.Application
{
    public static class AppService
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            //services
            services.AddScoped<IUserService,UserService>();
            services.AddScoped<IProfileService,ProfileService>();
            services.AddScoped<ISkillService,SkillService>();
            services.AddScoped<IUserSkillService, UserSkillService>();
            services.AddScoped<IMatchService, MatchService>();
            services.AddScoped<IKanbanBoardService, KanbanBoardService>();
            services.AddScoped<IKanbanTaskService, KanbanTaskService>();
            services.AddScoped<IKanbanTaskAnswerService, KanbanTaskAnswerService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IMatchSwipeService, MatchSwipeService>();
            services.AddScoped<IMatchSuggestionService, MatchSuggestionService>();





            //mappers
            services.AddAutoMapper([
                typeof(UserMapper),
                typeof(UserSkillMapper),
                typeof(SkillMapper),
                typeof(ProfileMapper),
                typeof(MatchMapper),
                typeof(KanbanTaskMapper),
                typeof(KanbanTaskAnswerMapper),
                typeof(KanbanBoardMapper),
                typeof(SessionMapper),
                typeof(ReviewMapper)
                ]);
            return services;
        }

    }
}
