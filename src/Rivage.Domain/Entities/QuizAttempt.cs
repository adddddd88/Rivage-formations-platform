namespace Rivage.Domain.Entities;

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal ScorePercent { get; set; }
    public int EarnedPoints { get; set; }
    public int MaxPoints { get; set; }
    public bool Passed { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<QuizAttemptAnswer> Answers { get; set; } = [];
}
