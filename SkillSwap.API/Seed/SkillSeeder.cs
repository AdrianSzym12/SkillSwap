using SkillSwap.Domain.Entities.Database;
using SkillSwap.Domain.Enums;
using SkillSwap.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SkillSwap.API.Seed
{
    public static class SkillSeeder
    {
        public static async Task SeedAsync(PersistenceContext db)
        {
            // nie seeduj, jeśli już są dane
            if (await db.Skills.AnyAsync())
                return;

            var now = DateTime.UtcNow;

            var skills = new List<Skill>
            {
                new Skill { Name="C#", Description="Programowanie w C#", Category=(SkillCategory)0, Tags="dotnet,backend", IsDeleted=false },
                new Skill { Name="ASP.NET Core", Description="Tworzenie API w .NET", Category=(SkillCategory)0, Tags="api,backend", IsDeleted=false },
                new Skill { Name="SQL", Description="Zapytania i modelowanie baz danych", Category=(SkillCategory)0, Tags="mysql,postgres", IsDeleted=false },
                new Skill { Name="JavaScript", Description="Podstawy JS", Category=(SkillCategory)0, Tags="frontend", IsDeleted=false },
                new Skill { Name="React", Description="Frontend w React", Category=(SkillCategory)0, Tags="frontend", IsDeleted=false },

                new Skill { Name="Angielski", Description="Nauka języka angielskiego", Category=(SkillCategory)0, Tags="language", IsDeleted=false },
                new Skill { Name="Niemiecki", Description="Nauka języka niemieckiego", Category=(SkillCategory)0, Tags="language", IsDeleted=false },

                new Skill { Name="Grafika", Description="Podstawy grafiki", Category=(SkillCategory)0, Tags="design", IsDeleted=false },
                new Skill { Name="Figma", Description="Projektowanie UI/UX", Category=(SkillCategory)0, Tags="ui,ux", IsDeleted=false },

                new Skill { Name="Excel", Description="Arkusze kalkulacyjne", Category=(SkillCategory)0, Tags="office", IsDeleted=false },
                new Skill { Name="PowerPoint", Description="Prezentacje", Category=(SkillCategory)0, Tags="office", IsDeleted=false },
            };


            // jeśli masz CreatedAt/UpdatedAt w Skill – ustaw tutaj
            // foreach (var s in skills) { s.CreatedAt = now; s.UpdatedAt = now; }

            await db.Skills.AddRangeAsync(skills);
            await db.SaveChangesAsync();
        }
    }
}
