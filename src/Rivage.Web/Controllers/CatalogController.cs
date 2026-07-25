using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Infrastructure.Data;

namespace Rivage.Web.Controllers;

public class CatalogController : Controller
{
    private readonly RivageDbContext _db;

    public CatalogController(RivageDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, int? categoryId, int page = 1)
    {
        page = Math.Max(1, page);
        const int pageSize = 9;

        var query = _db.Formations.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.TrainerProfile)!.ThenInclude(t => t!.User)
            .Where(f => f.IsPublished);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(f => f.Title.Contains(term) || f.ShortDescription.Contains(term));
        }

        if (categoryId is > 0)
            query = query.Where(f => f.CategoryId == categoryId);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Search = q;
        ViewBag.CategoryId = categoryId;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;

        return View(items);
    }

    public async Task<IActionResult> Details(string slug)
    {
        var formation = await _db.Formations.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.TrainerProfile)!.ThenInclude(t => t!.User)
            .Include(f => f.Modules.Where(m => m.IsPublished).OrderBy(m => m.OrderIndex))
            .FirstOrDefaultAsync(f => f.Slug == slug && f.IsPublished);

        if (formation is null) return NotFound();
        return View(formation);
    }
}
