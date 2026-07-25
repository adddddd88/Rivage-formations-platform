using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Infrastructure.Services;
using Rivage.Web.ViewModels;

namespace Rivage.Web.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin)]
public class FormationsController : Controller
{
    private readonly RivageDbContext _db;

    public FormationsController(RivageDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        page = Math.Max(1, page);
        const int pageSize = 10;
        var query = _db.Formations.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.TrainerProfile)!.ThenInclude(t => t!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Title.Contains(search));
        if (categoryId is > 0)
            query = query.Where(f => f.CategoryId == categoryId);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(f => f.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", categoryId);
        return View("~/Views/Admin/Formations/Index.cshtml", new PagedResult<Formation>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total, Search = search
        });
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        return View("~/Views/Admin/Formations/Edit.cshtml", new FormationEditViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FormationEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookups(model.CategoryId, model.TrainerProfileId);
            return View("~/Views/Admin/Formations/Edit.cshtml", model);
        }

        var entity = Map(model);
        entity.Slug = await UniqueSlugAsync(SlugHelper.Generate(model.Title));
        entity.CreatedAt = DateTime.UtcNow;
        _db.Formations.Add(entity);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Formation créée.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _db.Formations.FindAsync(id);
        if (entity is null) return NotFound();
        await LoadLookups(entity.CategoryId, entity.TrainerProfileId);
        return View("~/Views/Admin/Formations/Edit.cshtml", new FormationEditViewModel
        {
            Id = entity.Id,
            Title = entity.Title,
            ShortDescription = entity.ShortDescription,
            Description = entity.Description,
            CategoryId = entity.CategoryId,
            TrainerProfileId = entity.TrainerProfileId,
            DurationHours = entity.DurationHours,
            Level = entity.Level,
            IsPublished = entity.IsPublished
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FormationEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await LoadLookups(model.CategoryId, model.TrainerProfileId);
            return View("~/Views/Admin/Formations/Edit.cshtml", model);
        }

        var entity = await _db.Formations.FindAsync(id);
        if (entity is null) return NotFound();
        entity.Title = model.Title;
        entity.ShortDescription = model.ShortDescription;
        entity.Description = model.Description;
        entity.CategoryId = model.CategoryId;
        entity.TrainerProfileId = model.TrainerProfileId;
        entity.DurationHours = model.DurationHours;
        entity.Level = model.Level;
        entity.IsPublished = model.IsPublished;
        entity.Slug = await UniqueSlugAsync(SlugHelper.Generate(model.Title), id);
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Formation mise à jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Formations.FindAsync(id);
        if (entity is null) return NotFound();
        _db.Formations.Remove(entity);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Formation supprimée.";
        return RedirectToAction(nameof(Index));
    }

    private static Formation Map(FormationEditViewModel model) => new()
    {
        Title = model.Title,
        ShortDescription = model.ShortDescription,
        Description = model.Description,
        CategoryId = model.CategoryId,
        TrainerProfileId = model.TrainerProfileId,
        DurationHours = model.DurationHours,
        Level = model.Level,
        IsPublished = model.IsPublished
    };

    private async Task LoadLookups(int? categoryId = null, int? trainerId = null)
    {
        ViewBag.Categories = new SelectList(await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "Id", "Name", categoryId);
        var trainers = await _db.TrainerProfiles.Include(t => t.User).Where(t => t.IsActive).ToListAsync();
        ViewBag.Trainers = new SelectList(trainers.Select(t => new { t.Id, Name = t.User.FullName }), "Id", "Name", trainerId);
    }

    private async Task<string> UniqueSlugAsync(string slug, int? excludeId = null)
    {
        var baseSlug = slug;
        var i = 1;
        while (await _db.Formations.AnyAsync(f => f.Slug == slug && (!excludeId.HasValue || f.Id != excludeId)))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }
}