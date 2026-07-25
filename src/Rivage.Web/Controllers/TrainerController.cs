using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;

namespace Rivage.Web.Controllers;

[Authorize(Roles = AppRoles.Trainer + "," + AppRoles.Admin)]
public class TrainerController : Controller
{
    private readonly RivageDbContext _db;

    public TrainerController(RivageDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var profile = await _db.TrainerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == UserId);

        if (profile is null && !User.IsInRole(AppRoles.Admin))
            return View("NoProfile");

        var formationsQuery = _db.Formations.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Enrollments)
            .Include(f => f.Modules)
            .AsQueryable();

        if (profile is not null && !User.IsInRole(AppRoles.Admin))
            formationsQuery = formationsQuery.Where(f => f.TrainerProfileId == profile.Id);

        var formations = await formationsQuery.OrderBy(f => f.Title).ToListAsync();
        return View(formations);
    }

    public async Task<IActionResult> Formation(int id)
    {
        var formation = await _db.Formations.AsNoTracking()
            .Include(f => f.Modules.OrderBy(m => m.OrderIndex))
            .Include(f => f.Enrollments).ThenInclude(e => e.User)
            .Include(f => f.TrainerProfile)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (formation is null) return NotFound();
        if (!User.IsInRole(AppRoles.Admin))
        {
            var profile = await _db.TrainerProfiles.AsNoTracking().FirstOrDefaultAsync(t => t.UserId == UserId);
            if (profile is null || formation.TrainerProfileId != profile.Id)
                return Forbid();
        }

        var quizIds = await _db.Quizzes.Where(q => q.Module.FormationId == id).Select(q => q.Id).ToListAsync();
        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Quiz)
            .Where(a => quizIds.Contains(a.QuizId))
            .OrderByDescending(a => a.SubmittedAt)
            .Take(30)
            .ToListAsync();

        ViewBag.Attempts = attempts;
        return View(formation);
    }
}
