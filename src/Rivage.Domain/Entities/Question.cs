using Rivage.Domain.Enums;

namespace Rivage.Domain.Entities;

public class Question
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<AnswerOption> Options { get; set; } = [];
}