using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Web.ViewModels;

namespace Rivage.Web.Controllers.Admin;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Trainer)]
public class QuizzesController : Controller
{
    private readonly RivageDbContext _db;

    public QuizzesController(RivageDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var quizzes = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Module).ThenInclude(m => m.Formation)
            .Include(q => q.Questions)
            .OrderBy(q => q.Module.Formation.Title)
            .ToListAsync();
        return View("~/Views/Admin/Quizzes/Index.cshtml", quizzes);
    }

    public async Task<IActionResult> Questions(int id)
    {
        var quiz = await _db.Quizzes.AsNoTracking()
            .Include(q => q.Module)
            .Include(q => q.Questions.OrderBy(x => x.OrderIndex)).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
        return quiz is null ? NotFound() : View("~/Views/Admin/Quizzes/Questions.cshtml", quiz);
    }

    public async Task<IActionResult> CreateQuestion(int quizId)
    {
        var quiz = await _db.Quizzes.FindAsync(quizId);
        if (quiz is null) return NotFound();
        var count = await _db.Questions.CountAsync(q => q.QuizId == quizId);
        return View("~/Views/Admin/Quizzes/EditQuestion.cshtml", new QuizQuestionEditViewModel
        {
            QuizId = quizId,
            OrderIndex = count + 1,
            Options =
            [
                new() { Text = "" },
                new() { Text = "" },
                new() { Text = "" },
                new() { Text = "" }
            ]
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(QuizQuestionEditViewModel model)
    {
        NormalizeTrueFalse(model);
        if (!model.Options.Any(o => !string.IsNullOrWhiteSpace(o.Text) && o.IsCorrect))
            ModelState.AddModelError(string.Empty, "Au moins une option correcte est requise.");
        if (!ModelState.IsValid) return View("~/Views/Admin/Quizzes/EditQuestion.cshtml", model);

        var question = new Question
        {
            QuizId = model.QuizId,
            Text = model.Text,
            Type = model.Type,
            Points = model.Points,
            OrderIndex = model.OrderIndex
        };
        foreach (var opt in model.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).Select((o, i) => (o, i)))
        {
            question.Options.Add(new AnswerOption
            {
                Text = opt.o.Text,
                IsCorrect = opt.o.IsCorrect,
                OrderIndex = opt.i + 1
            });
        }

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Question ajoutée.";
        return RedirectToAction(nameof(Questions), new { id = model.QuizId });
    }

    public async Task<IActionResult> EditQuestion(int id)
    {
        var question = await _db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();

        var model = new QuizQuestionEditViewModel
        {
            Id = question.Id,
            QuizId = question.QuizId,
            Text = question.Text,
            Type = question.Type,
            Points = question.Points,
            OrderIndex = question.OrderIndex,
            Options = question.Options.OrderBy(o => o.OrderIndex)
                .Select(o => new QuizQuestionEditViewModel.OptionEdit { Id = o.Id, Text = o.Text, IsCorrect = o.IsCorrect })
                .ToList()
        };
        while (model.Options.Count < 4)
            model.Options.Add(new());
        return View("~/Views/Admin/Quizzes/EditQuestion.cshtml", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(int id, QuizQuestionEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        NormalizeTrueFalse(model);
        if (!model.Options.Any(o => !string.IsNullOrWhiteSpace(o.Text) && o.IsCorrect))
            ModelState.AddModelError(string.Empty, "Au moins une option correcte est requise.");
        if (!ModelState.IsValid) return View("~/Views/Admin/Quizzes/EditQuestion.cshtml", model);

        var question = await _db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == id);
        if (question is null) return NotFound();

        question.Text = model.Text;
        question.Type = model.Type;
        question.Points = model.Points;
        question.OrderIndex = model.OrderIndex;
        _db.AnswerOptions.RemoveRange(question.Options);
        question.Options.Clear();
        foreach (var opt in model.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)).Select((o, i) => (o, i)))
        {
            question.Options.Add(new AnswerOption
            {
                Text = opt.o.Text,
                IsCorrect = opt.o.IsCorrect,
                OrderIndex = opt.i + 1
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Question mise à jour.";
        return RedirectToAction(nameof(Questions), new { id = model.QuizId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _db.Questions.FindAsync(id);
        if (question is null) return NotFound();
        var quizId = question.QuizId;
        _db.Questions.Remove(question);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Question supprimée.";
        return RedirectToAction(nameof(Questions), new { id = quizId });
    }

    private static void NormalizeTrueFalse(QuizQuestionEditViewModel model)
    {
        if (model.Type != QuestionType.TrueFalse) return;
        model.Options =
        [
            new() { Text = "Vrai", IsCorrect = model.Options.ElementAtOrDefault(0)?.IsCorrect == true },
            new() { Text = "Faux", IsCorrect = model.Options.ElementAtOrDefault(0)?.IsCorrect != true }
        ];
    }
}
