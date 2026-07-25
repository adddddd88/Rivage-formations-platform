namespace Rivage.Domain.Entities;

public class Exercise
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string? SolutionHint { get; set; }
    public int MaxScore { get; set; } = 100;

    public Module Module { get; set; } = null!;
}
