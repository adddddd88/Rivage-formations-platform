using Rivage.Domain.Enums;

namespace Rivage.Domain.Entities;

public class Enrollment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int FormationId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public decimal ProgressPercent { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Formation Formation { get; set; } = null!;
    public ICollection<ModuleProgress> ModuleProgresses { get; set; } = [];
    public Certificate? Certificate { get; set; }
}
