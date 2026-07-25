using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;

namespace Rivage.Web.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin)]
public class AdminDashboardController : Controller
{
    private readonly RivageDbContext _db;

    public AdminDashboardController(RivageDbContext db) => _db = db;

    [HttpGet("/Admin")]
    [HttpGet("/Admin/Dashboard")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Formations = await _db.Formations.CountAsync();
        ViewBag.Learners = await _db.Users.CountAsync();
        ViewBag.Enrollments = await _db.Enrollments.CountAsync();
        ViewBag.Categories = await _db.Categories.CountAsync();
        ViewBag.Quizzes = await _db.Quizzes.CountAsync();
        ViewBag.RecentEnrollments = await _db.Enrollments.AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Formation)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(8)
            .ToListAsync();
        return View("~/Views/Admin/Dashboard/Index.cshtml");
    }
}
