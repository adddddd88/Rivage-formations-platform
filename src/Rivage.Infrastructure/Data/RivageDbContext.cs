using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;

namespace Rivage.Infrastructure.Data;

public class RivageDbContext : IdentityDbContext<ApplicationUser>
{
    public RivageDbContext(DbContextOptions<RivageDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TrainerProfile> TrainerProfiles => Set<TrainerProfile>();
    public DbSet<Formation> Formations => Set<Formation>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ModuleProgress> ModuleProgresses => Set<ModuleProgress>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        });

        builder.Entity<Formation>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(400);
            e.Property(x => x.Level).HasMaxLength(40);
            e.HasOne(x => x.Category).WithMany(c => c.Formations).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TrainerProfile).WithMany(t => t.Formations).HasForeignKey(x => x.TrainerProfileId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Module>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Formation).WithMany(f => f.Modules).HasForeignKey(x => x.FormationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FormationId, x.OrderIndex });
        });

        builder.Entity<Exercise>(e =>
        {
            e.HasOne(x => x.Module).WithOne(m => m.Exercise).HasForeignKey<Exercise>(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Quiz>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Module).WithOne(m => m.Quiz).HasForeignKey<Quiz>(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Question>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.Quiz).WithMany(q => q.Questions).HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AnswerOption>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.Question).WithMany(q => q.Options).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TrainerProfile>(e =>
        {
            e.HasOne(x => x.User).WithOne(u => u.TrainerProfile).HasForeignKey<TrainerProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<Enrollment>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.FormationId }).IsUnique();
            e.Property(x => x.ProgressPercent).HasPrecision(5, 2);
            e.HasOne(x => x.User).WithMany(u => u.Enrollments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Formation).WithMany(f => f.Enrollments).HasForeignKey(x => x.FormationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ModuleProgress>(e =>
        {
            e.HasIndex(x => new { x.EnrollmentId, x.ModuleId }).IsUnique();
            e.HasOne(x => x.Enrollment).WithMany(en => en.ModuleProgresses).HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Module).WithMany(m => m.ProgressRecords).HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuizAttempt>(e =>
        {
            e.Property(x => x.ScorePercent).HasPrecision(5, 2);
            e.HasOne(x => x.Quiz).WithMany(q => q.Attempts).HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizAttemptAnswer>(e =>
        {
            e.HasOne(x => x.QuizAttempt).WithMany(a => a.Answers).HasForeignKey(x => x.QuizAttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SelectedOption).WithMany().HasForeignKey(x => x.SelectedOptionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Certificate>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Enrollment).WithOne(en => en.Certificate).HasForeignKey<Certificate>(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        });
    }
}
