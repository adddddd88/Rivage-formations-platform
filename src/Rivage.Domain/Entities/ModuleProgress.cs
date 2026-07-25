namespace Rivage.Domain.Entities;

public class ModuleProgress
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int ModuleId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ExerciseResponse { get; set; }
    public int? ExerciseScore { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
    public Module Module { get; set; } = null!;
}
