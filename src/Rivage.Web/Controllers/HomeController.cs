using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Infrastructure.Data;

namespace Rivage.Web.Controllers;

public class HomeController : Controller
{
    private readonly RivageDbContext _db;

    public HomeController(RivageDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var formations = await _db.Formations.AsNoTracking()
            .Include(f => f.Category)
            .Where(f => f.IsPublished)
            .OrderByDescending(f => f.CreatedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.FormationCount = await _db.Formations.CountAsync(f => f.IsPublished);
        ViewBag.LearnerCount = await _db.Enrollments.Select(e => e.UserId).Distinct().CountAsync();
        return View(formations);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    [ActionName("StatusCode")]
    public IActionResult StatusCodePage(int code)
    {
        ViewBag.Code = code;
        return View("StatusCode");
    }
}
