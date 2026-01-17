
using Microsoft.EntityFrameworkCore;
using SkillSwap.API.Extensions;
using SkillSwap.API.Middleware;
using SkillSwap.Application;
using SkillSwap.Application.Commons;
using SkillSwap.Application.AuthExtensions;
using SkillSwap.Application.Mappers;
using SkillSwap.Domain.Entities.Config;
using SkillSwap.Infrastructure;
using SkillSwap.Persistence;
using SkillSwap.API.Seed;

namespace SkillSwap.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Configure(builder);

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PersistenceContext>();
                if(dbContext.Database.GetPendingMigrations().Any())
                    dbContext.Database.Migrate();
            }
            // Seeder
            /*using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PersistenceContext>();
                await SkillSeeder.SeedAsync(db);
            }*/

            app.UseErrorHandling();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
            //Server = localhost; Port = 3306; Database = SkillSwapDb; User = root;
        }

        private static void Configure(WebApplicationBuilder webApplicationBuilder) =>
            webApplicationBuilder.Host.ConfigureAppConfiguration(configuration =>
            {
                var config = configuration
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile(
                        AppConstants.Configuration.APPSETTINGS_FILE, 
                        optional: false, 
                        reloadOnChange: true)
                    .Build();

                config.GetSection(nameof(Configuration)).Get<Configuration>();
            }).ConfigureServices((context, services) =>
            {
                var config = context.Configuration.GetSection(nameof(Configuration)).Get<Configuration>();

                
                //Api
                services.AddControllers();
                services.AddEndpointsApiExplorer();
                //services.AddSwaggerGen();
                services.AddSwaggerWithJwt();


                services.AddAutoMapper(
                    typeof(ProfileMapper), 
                    typeof(KanbanBoardMapper),
                    typeof(UserMapper), 
                    typeof(KanbanTaskAnswerMapper),
                    typeof(KanbanTaskMapper), 
                    typeof(UserSkillMapper),
                    typeof(SkillMapper), 
                    typeof(MatchMapper),
                    typeof(SessionMapper));

                services
                    .AddSingleton(config)
                    .AddApplication()
                    .AddInfrastructureServices()
                    .AddPersistence()
                    .AddJwtAuthentication(config);
            });
    }
}
