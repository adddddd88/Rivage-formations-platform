namespace Rivage.Domain.Entities;

public class Certificate
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string LearnerName { get; set; } = string.Empty;
    public string FormationTitle { get; set; } = string.Empty;

    public Enrollment Enrollment { get; set; } = null!;
}
