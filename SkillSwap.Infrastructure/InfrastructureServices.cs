using Microsoft.Extensions.DependencyInjection;
using SkillSwap.Infrastructure.MatchLogic;

namespace SkillSwap.Infrastructure
{
    public static class InfrastructureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<MatchSuggestion>();

            return services;
        }
    }
}
