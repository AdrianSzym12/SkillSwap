using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Application.Interfaces;
using SkillSwap.Persistence.MatchLogic;

namespace SkillSwap.Infrastructure
{
    public static class InfrastructureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IMatchSuggestion, MatchSuggestion>();

            return services;
        }
    }
}
