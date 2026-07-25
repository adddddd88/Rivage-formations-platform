using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Infrastructure.Services;

namespace Rivage.Tests.Unit;

public class QuizScoringLogicTests
{
    private static RivageDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RivageDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RivageDbContext(options);
    }

    private static async Task<(Quiz quiz, ApplicationUser user, Dictionary<int, int> correctMap)> SeedQuizAsync(
        RivageDbContext db, int passingScore = 70)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "learner@test.local",
            Email = "learner@test.local",
            FirstName = "Test",
            LastName = "Learner",
            NormalizedUserName = "LEARNER@TEST.LOCAL",
            NormalizedEmail = "LEARNER@TEST.LOCAL"
        };
        db.Users.Add(user);

        var category = new Category { Name = "Cat", Slug = "cat" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var formation = new Formation
        {
            Title = "Formation",
            Slug = "formation",
            CategoryId = category.Id,
            IsPublished = true,
            ShortDescription = "s",
            Description = "d"
        };
        db.Formations.Add(formation);
        await db.SaveChangesAsync();

        var module = new Module
        {
            FormationId = formation.Id,
            Title = "Quiz module",
            OrderIndex = 1,
            ContentType = ModuleContentType.Quiz,
            IsPublished = true
        };
        db.Modules.Add(module);
        await db.SaveChangesAsync();

        var q1Correct = new AnswerOption { Text = "Good", IsCorrect = true, OrderIndex = 1 };
        var q1Wrong = new AnswerOption { Text = "Bad", IsCorrect = false, OrderIndex = 2 };
        var q2Correct = new AnswerOption { Text = "Vrai", IsCorrect = true, OrderIndex = 1 };
        var q2Wrong = new AnswerOption { Text = "Faux", IsCorrect = false, OrderIndex = 2 };

        var quiz = new Quiz
        {
            ModuleId = module.Id,
            Title = "Test quiz",
            PassingScorePercent = passingScore,
            Questions =
            [
                new Question
                {
                    Text = "Q1",
                    Points = 2,
                    OrderIndex = 1,
                    Type = QuestionType.MultipleChoice,
                    Options = [q1Correct, q1Wrong]
                },
                new Question
                {
                    Text = "Q2",
                    Points = 1,
                    OrderIndex = 2,
                    Type = QuestionType.TrueFalse,
                    Options = [q2Correct, q2Wrong]
                }
            ]
        };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        var map = new Dictionary<int, int>
        {
            [quiz.Questions.ElementAt(0).Id] = quiz.Questions.ElementAt(0).Options.First(o => o.IsCorrect).Id,
            [quiz.Questions.ElementAt(1).Id] = quiz.Questions.ElementAt(1).Options.First(o => o.IsCorrect).Id
        };

        return (quiz, user, map);
    }

    [Fact]
    public async Task SubmitAsync_perfect_score_passes()
    {
        await using var db = CreateDb();
        var (quiz, user, correct) = await SeedQuizAsync(db);
        var sut = new QuizScoringService(db);

        var attempt = await sut.SubmitAsync(quiz.Id, user.Id, correct);

        attempt.MaxPoints.Should().Be(3);
        attempt.EarnedPoints.Should().Be(3);
        attempt.ScorePercent.Should().Be(100m);
        attempt.Passed.Should().BeTrue();
        attempt.Answers.Should().HaveCount(2);
        attempt.Answers.Should().OnlyContain(a => a.IsCorrect && a.PointsAwarded > 0);
    }

    [Fact]
    public async Task SubmitAsync_partial_score_rounds_to_two_decimals()
    {
        await using var db = CreateDb();
        var (quiz, user, correct) = await SeedQuizAsync(db, passingScore: 70);
        var sut = new QuizScoringService(db);

        // Only first question (2 of 3 points) => 66.67%
        var partial = new Dictionary<int, int>
        {
            [quiz.Questions.ElementAt(0).Id] = correct[quiz.Questions.ElementAt(0).Id]
        };

        var attempt = await sut.SubmitAsync(quiz.Id, user.Id, partial);

        attempt.EarnedPoints.Should().Be(2);
        attempt.MaxPoints.Should().Be(3);
        attempt.ScorePercent.Should().Be(66.67m);
        attempt.Passed.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAsync_wrong_answers_zero_points()
    {
        await using var db = CreateDb();
        var (quiz, user, _) = await SeedQuizAsync(db);
        var sut = new QuizScoringService(db);

        var wrong = quiz.Questions.ToDictionary(
            q => q.Id,
            q => q.Options.First(o => !o.IsCorrect).Id);

        var attempt = await sut.SubmitAsync(quiz.Id, user.Id, wrong);

        attempt.EarnedPoints.Should().Be(0);
        attempt.ScorePercent.Should().Be(0m);
        attempt.Passed.Should().BeFalse();
    }

    [Fact]
    public void Score_percent_math_matches_expected_formula()
    {
        // Pure logic mirror of QuizScoringService formula
        var earned = 2;
        var max = 3;
        var score = max == 0 ? 0 : Math.Round((decimal)earned / max * 100m, 2);
        score.Should().Be(66.67m);
        (score >= 70).Should().BeFalse();
        (Math.Round(3m / 3m * 100m, 2) >= 70).Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_passing_marks_module_completed_when_enrolled()
    {
        await using var db = CreateDb();
        var (quiz, user, correct) = await SeedQuizAsync(db);
        var module = await db.Modules.FirstAsync(m => m.Id == quiz.ModuleId);

        db.Enrollments.Add(new Enrollment
        {
            UserId = user.Id,
            FormationId = module.FormationId,
            Status = EnrollmentStatus.Active
        });
        await db.SaveChangesAsync();

        var sut = new QuizScoringService(db);
        var attempt = await sut.SubmitAsync(quiz.Id, user.Id, correct);

        attempt.Passed.Should().BeTrue();
        var progress = await db.ModuleProgresses.SingleAsync(p => p.ModuleId == module.Id);
        progress.IsCompleted.Should().BeTrue();

        var enrollment = await db.Enrollments.SingleAsync();
        enrollment.ProgressPercent.Should().Be(100m);
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }
}
