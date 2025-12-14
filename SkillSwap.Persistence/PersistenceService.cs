
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Domain.Entities.Config;
using SkillSwap.Domain.Interfaces;
using SkillSwap.Persistence.Repositories;



namespace SkillSwap.Persistence
{
    public static class PersistenceService
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<Configuration>();
            var connectionString = $"Server = {configuration.Api.Server}; Port ={configuration.Api.Port}; Database = {configuration.Api.Database}; User = {configuration.Api.User}; Password = {configuration.Api.Password};";

            services.AddDbContext<PersistenceContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

            services.AddScoped<IUserRepository,UserRepository>();
            services.AddScoped<IProfileRepository,ProfileRepository>();
            services.AddScoped<ISkillRepository,SkillRepository>();
            services.AddScoped<IUserSkillRepository,UserSkillRepository>();
            services.AddScoped<IKanbanBoardRepository,KanbanBoardRepository>();
            services.AddScoped<IKanbanTaskAnswerRepository,KanbanTaskAnswerRepository>();
            services.AddScoped<IKanbanTaskRepository,KanbanTaskRepository>();
            services.AddScoped<IMatchRepository, MatchRepository>();
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IMatchSwipeRepository, MatchSwipeRepository>();



            return services;
        }
    }
}
