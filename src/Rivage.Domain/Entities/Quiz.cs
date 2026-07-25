namespace Rivage.Domain.Entities;

public class Quiz
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int PassingScorePercent { get; set; } = 70;
    public int? TimeLimitMinutes { get; set; }
    public bool ShowResultsImmediately { get; set; } = true;

    public Module Module { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<QuizAttempt> Attempts { get; set; } = [];
}
