using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Infrastructure.Data;

namespace Rivage.Infrastructure.Services;

public class QuizScoringService
{
    private readonly RivageDbContext _db;

    public QuizScoringService(RivageDbContext db)
    {
        _db = db;
    }

    public async Task<QuizAttempt> SubmitAsync(
        int quizId,
        string userId,
        IDictionary<int, int> selectedOptionByQuestionId,
        CancellationToken cancellationToken = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .Include(q => q.Module)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken)
            ?? throw new InvalidOperationException("Quiz introuvable.");

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            MaxPoints = quiz.Questions.Sum(q => q.Points)
        };

        foreach (var question in quiz.Questions.OrderBy(q => q.OrderIndex))
        {
            selectedOptionByQuestionId.TryGetValue(question.Id, out var selectedId);
            var selected = question.Options.FirstOrDefault(o => o.Id == selectedId);
            var isCorrect = selected?.IsCorrect == true;
            var points = isCorrect ? question.Points : 0;

            attempt.Answers.Add(new QuizAttemptAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = selected?.Id,
                IsCorrect = isCorrect,
                PointsAwarded = points
            });

            attempt.EarnedPoints += points;
        }

        attempt.ScorePercent = attempt.MaxPoints == 0
            ? 0
            : Math.Round((decimal)attempt.EarnedPoints / attempt.MaxPoints * 100m, 2);
        attempt.Passed = attempt.ScorePercent >= quiz.PassingScorePercent;

        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        if (attempt.Passed)
        {
            await MarkModuleCompletedAsync(quiz.ModuleId, userId, cancellationToken);
        }

        return attempt;
    }

    private async Task MarkModuleCompletedAsync(int moduleId, string userId, CancellationToken cancellationToken)
    {
        var module = await _db.Modules.AsNoTracking().FirstAsync(m => m.Id == moduleId, cancellationToken);
        var enrollment = await _db.Enrollments
            .Include(e => e.ModuleProgresses)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FormationId == module.FormationId, cancellationToken);

        if (enrollment is null) return;

        var progress = enrollment.ModuleProgresses.FirstOrDefault(p => p.ModuleId == moduleId);
        if (progress is null)
        {
            progress = new ModuleProgress
            {
                EnrollmentId = enrollment.Id,
                ModuleId = moduleId,
                StartedAt = DateTime.UtcNow
            };
            _db.ModuleProgresses.Add(progress);
        }

        if (!progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;
        }

        await RecalculateProgressAsync(enrollment.Id, cancellationToken);
    }

    public async Task RecalculateProgressAsync(int enrollmentId, CancellationToken cancellationToken = default)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Formation).ThenInclude(f => f.Modules)
            .Include(e => e.ModuleProgresses)
            .Include(e => e.User)
            .Include(e => e.Certificate)
            .FirstAsync(e => e.Id == enrollmentId, cancellationToken);

        var total = enrollment.Formation.Modules.Count(m => m.IsPublished);
        var done = enrollment.ModuleProgresses.Count(p => p.IsCompleted);
        enrollment.ProgressPercent = total == 0 ? 0 : Math.Round((decimal)done / total * 100m, 2);

        if (total > 0 && done >= total)
        {
            enrollment.Status = Domain.Enums.EnrollmentStatus.Completed;
            enrollment.CompletedAt ??= DateTime.UtcNow;

            if (enrollment.Certificate is null)
            {
                enrollment.Certificate = new Certificate
                {
                    Code = $"RVG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    IssuedAt = DateTime.UtcNow,
                    LearnerName = enrollment.User.FullName,
                    FormationTitle = enrollment.Formation.Title
                };
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
