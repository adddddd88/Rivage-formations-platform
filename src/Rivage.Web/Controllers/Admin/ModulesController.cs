using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Web.ViewModels;

namespace Rivage.Web.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Trainer)]
public class ModulesController : Controller
{
    private readonly RivageDbContext _db;

    public ModulesController(RivageDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? formationId, string? search)
    {
        var query = _db.Modules.AsNoTracking()
            .Include(m => m.Formation)
            .AsQueryable();

        if (formationId is > 0)
            query = query.Where(m => m.FormationId == formationId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search));

        ViewBag.Formations = new SelectList(await _db.Formations.OrderBy(f => f.Title).ToListAsync(), "Id", "Title", formationId);
        ViewBag.Search = search;
        ViewBag.FormationId = formationId;
        var items = await query.OrderBy(m => m.Formation.Title).ThenBy(m => m.OrderIndex).ToListAsync();
        return View("~/Views/Admin/Modules/Index.cshtml", items);
    }

    public async Task<IActionResult> Create(int? formationId)
    {
        await LoadFormations(formationId);
        return View("~/Views/Admin/Modules/Edit.cshtml", new ModuleEditViewModel
        {
            FormationId = formationId ?? 0,
            QuizTitle = "Quiz du module"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ModuleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadFormations(model.FormationId);
            return View("~/Views/Admin/Modules/Edit.cshtml", model);
        }

        var module = MapModule(model);
        _db.Modules.Add(module);
        await _db.SaveChangesAsync();
        await UpsertSideContent(module, model);
        TempData["Success"] = "Module créé.";
        return RedirectToAction(nameof(Index), new { formationId = model.FormationId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var module = await _db.Modules.Include(m => m.Exercise).Include(m => m.Quiz)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (module is null) return NotFound();
        await LoadFormations(module.FormationId);
        return View("~/Views/Admin/Modules/Edit.cshtml", new ModuleEditViewModel
        {
            Id = module.Id,
            FormationId = module.FormationId,
            Title = module.Title,
            Summary = module.Summary,
            Content = module.Content,
            OrderIndex = module.OrderIndex,
            EstimatedMinutes = module.EstimatedMinutes,
            ContentType = module.ContentType,
            IsPublished = module.IsPublished,
            ExerciseInstructions = module.Exercise?.Instructions,
            ExerciseHint = module.Exercise?.SolutionHint,
            QuizTitle = module.Quiz?.Title ?? "Quiz du module",
            PassingScorePercent = module.Quiz?.PassingScorePercent ?? 70
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ModuleEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await LoadFormations(model.FormationId);
            return View("~/Views/Admin/Modules/Edit.cshtml", model);
        }

        var module = await _db.Modules.Include(m => m.Exercise).Include(m => m.Quiz)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (module is null) return NotFound();

        module.FormationId = model.FormationId;
        module.Title = model.Title;
        module.Summary = model.Summary;
        module.Content = model.Content;
        module.OrderIndex = model.OrderIndex;
        module.EstimatedMinutes = model.EstimatedMinutes;
        module.ContentType = model.ContentType;
        module.IsPublished = model.IsPublished;
        await _db.SaveChangesAsync();
        await UpsertSideContent(module, model);
        TempData["Success"] = "Module mis à jour.";
        return RedirectToAction(nameof(Index), new { formationId = model.FormationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var module = await _db.Modules.FindAsync(id);
        if (module is null) return NotFound();
        var formationId = module.FormationId;
        _db.Modules.Remove(module);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Module supprimé.";
        return RedirectToAction(nameof(Index), new { formationId });
    }

    private static Module MapModule(ModuleEditViewModel model) => new()
    {
        FormationId = model.FormationId,
        Title = model.Title,
        Summary = model.Summary,
        Content = model.Content,
        OrderIndex = model.OrderIndex,
        EstimatedMinutes = model.EstimatedMinutes,
        ContentType = model.ContentType,
        IsPublished = model.IsPublished
    };

    private async Task UpsertSideContent(Module module, ModuleEditViewModel model)
    {
        if (model.ContentType == ModuleContentType.Exercise)
        {
            if (module.Exercise is null)
            {
                module.Exercise = new Exercise { ModuleId = module.Id };
                _db.Exercises.Add(module.Exercise);
            }
            module.Exercise.Instructions = model.ExerciseInstructions ?? string.Empty;
            module.Exercise.SolutionHint = model.ExerciseHint;
        }

        if (model.ContentType == ModuleContentType.Quiz)
        {
            if (module.Quiz is null)
            {
                module.Quiz = new Quiz { ModuleId = module.Id };
                _db.Quizzes.Add(module.Quiz);
            }
            module.Quiz.Title = string.IsNullOrWhiteSpace(model.QuizTitle) ? module.Title : model.QuizTitle!;
            module.Quiz.PassingScorePercent = model.PassingScorePercent;
            module.Quiz.ShowResultsImmediately = true;
        }

        await _db.SaveChangesAsync();
    }

    private async Task LoadFormations(int? selected)
    {
        ViewBag.Formations = new SelectList(await _db.Formations.OrderBy(f => f.Title).ToListAsync(), "Id", "Title", selected);
    }
}
