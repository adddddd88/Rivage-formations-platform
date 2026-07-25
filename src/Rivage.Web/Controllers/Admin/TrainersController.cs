using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Web.ViewModels;

namespace Rivage.Web.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin)]
public class TrainersController : Controller
{
    private readonly RivageDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public TrainersController(RivageDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.TrainerProfiles.AsNoTracking().Include(t => t.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.User.FirstName.Contains(search) || t.User.LastName.Contains(search) || t.Specialty.Contains(search));
        return View("~/Views/Admin/Trainers/Index.cshtml", await query.OrderBy(t => t.User.LastName).ToListAsync());
    }

    public IActionResult Create() => View("~/Views/Admin/Trainers/Edit.cshtml", new TrainerEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainerEditViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Mot de passe requis pour créer un formateur.");
        if (!ModelState.IsValid) return View("~/Views/Admin/Trainers/Edit.cshtml", model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IsActive = model.IsActive
        };
        var result = await _users.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View("~/Views/Admin/Trainers/Edit.cshtml", model);
        }

        await _users.AddToRoleAsync(user, AppRoles.Trainer);
        _db.TrainerProfiles.Add(new TrainerProfile
        {
            UserId = user.Id,
            Bio = model.Bio,
            Specialty = model.Specialty,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Formateur créé.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var profile = await _db.TrainerProfiles.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (profile is null) return NotFound();
        return View("~/Views/Admin/Trainers/Edit.cshtml", new TrainerEditViewModel
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Email = profile.User.Email ?? string.Empty,
            FirstName = profile.User.FirstName,
            LastName = profile.User.LastName,
            Specialty = profile.Specialty,
            Bio = profile.Bio,
            IsActive = profile.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainerEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View("~/Views/Admin/Trainers/Edit.cshtml", model);

        var profile = await _db.TrainerProfiles.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
        if (profile is null) return NotFound();

        profile.User.FirstName = model.FirstName;
        profile.User.LastName = model.LastName;
        profile.User.Email = model.Email;
        profile.User.UserName = model.Email;
        profile.User.IsActive = model.IsActive;
        profile.Specialty = model.Specialty;
        profile.Bio = model.Bio;
        profile.IsActive = model.IsActive;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(profile.User);
            await _users.ResetPasswordAsync(profile.User, token, model.Password);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Formateur mis à jour.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var profile = await _db.TrainerProfiles.Include(t => t.Formations).FirstOrDefaultAsync(t => t.Id == id);
        if (profile is null) return NotFound();
        if (profile.Formations.Count > 0)
        {
            TempData["Error"] = "Désassignez d'abord les formations de ce formateur.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _users.FindByIdAsync(profile.UserId);
        _db.TrainerProfiles.Remove(profile);
        await _db.SaveChangesAsync();
        if (user is not null) await _users.DeleteAsync(user);
        TempData["Success"] = "Formateur supprimé.";
        return RedirectToAction(nameof(Index));
    }
}
