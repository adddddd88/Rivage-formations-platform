using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;

namespace Rivage.Infrastructure.Services;

public class EnrollmentService
{
    private readonly RivageDbContext _db;
    private readonly QuizScoringService _progress;

    public EnrollmentService(RivageDbContext db, QuizScoringService progress)
    {
        _db = db;
        _progress = progress;
    }

    public async Task<Enrollment> EnrollAsync(string userId, int formationId, CancellationToken cancellationToken = default)
    {
        var formation = await _db.Formations
            .Include(f => f.Modules)
            .FirstOrDefaultAsync(f => f.Id == formationId && f.IsPublished, cancellationToken)
            ?? throw new InvalidOperationException("Formation introuvable ou non publiée.");

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FormationId == formationId, cancellationToken);

        if (existing is not null)
            return existing;

        var enrollment = new Enrollment
        {
            UserId = userId,
            FormationId = formationId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(cancellationToken);
        return enrollment;
    }

    public async Task CompleteLessonAsync(string userId, int moduleId, CancellationToken cancellationToken = default)
    {
        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            ?? throw new InvalidOperationException("Module introuvable.");

        var enrollment = await _db.Enrollments
            .Include(e => e.ModuleProgresses)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FormationId == module.FormationId, cancellationToken)
            ?? throw new InvalidOperationException("Vous n'êtes pas inscrit à cette formation.");

        if (!await CanAccessModuleAsync(enrollment.Id, module, cancellationToken))
            throw new InvalidOperationException("Terminez le module précédent avant de continuer.");

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

        progress.IsCompleted = true;
        progress.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _progress.RecalculateProgressAsync(enrollment.Id, cancellationToken);
    }

    public async Task SubmitExerciseAsync(string userId, int moduleId, string response, CancellationToken cancellationToken = default)
    {
        var module = await _db.Modules.Include(m => m.Exercise)
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            ?? throw new InvalidOperationException("Module introuvable.");

        if (module.Exercise is null)
            throw new InvalidOperationException("Ce module n'a pas d'exercice.");

        var enrollment = await _db.Enrollments
            .Include(e => e.ModuleProgresses)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FormationId == module.FormationId, cancellationToken)
            ?? throw new InvalidOperationException("Vous n'êtes pas inscrit à cette formation.");

        if (!await CanAccessModuleAsync(enrollment.Id, module, cancellationToken))
            throw new InvalidOperationException("Terminez le module précédent avant de continuer.");

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

        progress.ExerciseResponse = response;
        progress.ExerciseScore = string.IsNullOrWhiteSpace(response) ? 0 : 80;
        progress.IsCompleted = !string.IsNullOrWhiteSpace(response);
        progress.CompletedAt = progress.IsCompleted ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync(cancellationToken);
        await _progress.RecalculateProgressAsync(enrollment.Id, cancellationToken);
    }

    public async Task<bool> CanAccessModuleAsync(int enrollmentId, Module module, CancellationToken cancellationToken = default)
    {
        var modules = await _db.Modules
            .Where(m => m.FormationId == module.FormationId && m.IsPublished)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync(cancellationToken);

        var previous = modules.Where(m => m.OrderIndex < module.OrderIndex).OrderByDescending(m => m.OrderIndex).FirstOrDefault();
        if (previous is null) return true;

        return await _db.ModuleProgresses.AnyAsync(
            p => p.EnrollmentId == enrollmentId && p.ModuleId == previous.Id && p.IsCompleted,
            cancellationToken);
    }
}