using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Infrastructure.Services;
using Rivage.Web.ViewModels;

namespace Rivage.Web.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin)]
public class CategoriesController : Controller
{
    private readonly RivageDbContext _db;

    public CategoriesController(RivageDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        page = Math.Max(1, page);
        const int pageSize = 10;
        var query = _db.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View("~/Views/Admin/Categories/Index.cshtml", new PagedResult<Category>
        {
            Items = items, Page = page, PageSize = pageSize, TotalCount = total, Search = search
        });
    }

    public IActionResult Create() => View("~/Views/Admin/Categories/Edit.cshtml", new CategoryEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryEditViewModel model)
    {
        if (!ModelState.IsValid) return View("~/Views/Admin/Categories/Edit.cshtml", model);
        _db.Categories.Add(new Category
        {
            Name = model.Name,
            Description = model.Description,
            Slug = await UniqueSlugAsync(SlugHelper.Generate(model.Name)),
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Catégorie créée.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return NotFound();
        return View("~/Views/Admin/Categories/Edit.cshtml", new CategoryEditViewModel
        {
            Id = entity.Id, Name = entity.Name, Description = entity.Description, IsActive = entity.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View("~/Views/Admin/Categories/Edit.cshtml", model);
        var entity = await _db.Categories.FindAsync(id);
        if (entity is null) return NotFound();
        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.IsActive = model.IsActive;
        entity.Slug = await UniqueSlugAsync(SlugHelper.Generate(model.Name), id);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Catégorie mise à jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Categories.Include(c => c.Formations).FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return NotFound();
        if (entity.Formations.Count > 0)
        {
            TempData["Error"] = "Impossible de supprimer : des formations y sont liées.";
            return RedirectToAction(nameof(Index));
        }
        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Catégorie supprimée.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> UniqueSlugAsync(string slug, int? excludeId = null)
    {
        var baseSlug = slug;
        var i = 1;
        while (await _db.Categories.AnyAsync(c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId)))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }
}
