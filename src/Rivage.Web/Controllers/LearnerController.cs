using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Infrastructure.Services;

namespace Rivage.Web.Controllers;

[Authorize(Roles = AppRoles.Learner + "," + AppRoles.Admin)]
public class LearnerController : Controller
{
    private readonly RivageDbContext _db;
    private readonly EnrollmentService _enrollments;
    private readonly QuizScoringService _quizzes;

    public LearnerController(RivageDbContext db, EnrollmentService enrollments, QuizScoringService quizzes)
    {
        _db = db;
        _enrollments = enrollments;
        _quizzes = quizzes;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var enrollments = await _db.Enrollments.AsNoTracking()
            .Include(e => e.Formation).ThenInclude(f => f.Category)
            .Include(e => e.Certificate)
            .Where(e => e.UserId == UserId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Include(a => a.Quiz).ThenInclude(q => q.Module)
            .Where(a => a.UserId == UserId)
            .OrderByDescending(a => a.SubmittedAt)
            .Take(8)
            .ToListAsync();

        ViewBag.Attempts = attempts;
        return View(enrollments);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int formationId)
    {
        try
        {
            await _enrollments.EnrollAsync(UserId, formationId);
            TempData["Success"] = "Inscription confirmée. Bon voyage d'apprentissage !";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        var formation = await _db.Formations.AsNoTracking().FirstOrDefaultAsync(f => f.Id == formationId);
        return formation is null
            ? RedirectToAction("Index", "Catalog")
            : RedirectToAction("Details", "Catalog", new { slug = formation.Slug });
    }

    public async Task<IActionResult> Formation(int id)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Formation).ThenInclude(f => f.Modules.Where(m => m.IsPublished).OrderBy(m => m.OrderIndex))
            .Include(e => e.ModuleProgresses)
            .Include(e => e.Certificate)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == UserId);

        if (enrollment is null) return NotFound();
        return View(enrollment);
    }

    public async Task<IActionResult> Module(int id)
    {
        var module = await _db.Modules
            .Include(m => m.Formation)
            .Include(m => m.Exercise)
            .Include(m => m.Quiz)!.ThenInclude(q => q!.Questions.OrderBy(x => x.OrderIndex)).ThenInclude(q => q.Options.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(m => m.Id == id && m.IsPublished);

        if (module is null) return NotFound();

        var enrollment = await _db.Enrollments
            .Include(e => e.ModuleProgresses)
            .FirstOrDefaultAsync(e => e.UserId == UserId && e.FormationId == module.FormationId);

        if (enrollment is null)
        {
            TempData["Error"] = "Inscrivez-vous à la formation pour accéder aux modules.";
            return RedirectToAction("Details", "Catalog", new { slug = module.Formation.Slug });
        }

        if (!await _enrollments.CanAccessModuleAsync(enrollment.Id, module))
        {
            TempData["Error"] = "Terminez le module précédent avant de continuer.";
            return RedirectToAction(nameof(Formation), new { id = enrollment.Id });
        }

        var progress = enrollment.ModuleProgresses.FirstOrDefault(p => p.ModuleId == module.Id);
        if (progress is null)
        {
            _db.ModuleProgresses.Add(new Domain.Entities.ModuleProgress
            {
                EnrollmentId = enrollment.Id,
                ModuleId = module.Id,
                StartedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        ViewBag.Enrollment = enrollment;
        ViewBag.Progress = enrollment.ModuleProgresses.FirstOrDefault(p => p.ModuleId == module.Id);
        return View(module);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLesson(int moduleId)
    {
        try
        {
            await _enrollments.CompleteLessonAsync(UserId, moduleId);
            TempData["Success"] = "Module marqué comme terminé.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Module), new { id = moduleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExercise(int moduleId, string response)
    {
        try
        {
            await _enrollments.SubmitExerciseAsync(UserId, moduleId, response);
            TempData["Success"] = "Exercice enregistré.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Module), new { id = moduleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, int> answers)
    {
        try
        {
            var attempt = await _quizzes.SubmitAsync(quizId, UserId, answers ?? new Dictionary<int, int>());
            return RedirectToAction(nameof(QuizResult), new { id = attempt.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            var quiz = await _db.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quizId);
            return quiz is null ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Module), new { id = quiz.ModuleId });
        }
    }

    public async Task<IActionResult> QuizResult(int id)
    {
        var attempt = await _db.QuizAttempts.AsNoTracking()
            .Include(a => a.Quiz).ThenInclude(q => q.Module)
            .Include(a => a.Answers).ThenInclude(x => x.Question)
            .Include(a => a.Answers).ThenInclude(x => x.SelectedOption)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);

        return attempt is null ? NotFound() : View(attempt);
    }

    public async Task<IActionResult> Certificate(int id)
    {
        var cert = await _db.Certificates.AsNoTracking()
            .Include(c => c.Enrollment)
            .FirstOrDefaultAsync(c => c.Id == id && c.Enrollment.UserId == UserId);

        return cert is null ? NotFound() : View(cert);
    }
}