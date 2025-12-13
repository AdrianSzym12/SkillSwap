using Microsoft.EntityFrameworkCore;
using SkillSwap.Domain.Entities.Database;


namespace SkillSwap.Persistence
{
    public class PersistenceContext : DbContext
    {
        public DbSet<Skill> Skills { get; set; }
        public DbSet<KanbanBoard> KanbanBoards { get; set; }
        public DbSet<KanbanTask> KanbanTasks { get; set; }
        public DbSet<KanbanTaskAnswer> KanbanTaskAnswers { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<MatchSwipe> MatchSwipes { get; set; }


        public PersistenceContext(DbContextOptions<PersistenceContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Skill>()
                .ToTable(nameof(Skills))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<KanbanBoard>()
                .ToTable(nameof(KanbanBoards))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<KanbanTask>()
                .ToTable(nameof(KanbanTasks))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<KanbanTaskAnswer>()
                .ToTable(nameof(KanbanTaskAnswers))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<Match>()
                .ToTable(nameof(Matches))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<Profile>()
                .ToTable(nameof(Profiles))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<User>()
                .ToTable(nameof(Users))
                .HasKey(t => t.Id);

            modelBuilder
                .Entity<UserSkill>()
                .ToTable(nameof(UserSkills))
                .HasKey(t => t.Id);
            modelBuilder
                .Entity<Session>()
                .ToTable(nameof(Sessions))
                .HasKey(t => t.Id);
            modelBuilder.Entity<Profile>()
                .HasIndex(t => t.UserId)
                .IsUnique();
            modelBuilder
               .Entity<Review>()
               .ToTable(nameof(Reviews))
               .HasKey(t => t.Id);

            modelBuilder
                .Entity<MatchSwipe>()
                .ToTable(nameof(MatchSwipes))
                .HasKey(t => t.Id);
        }
    }
}
