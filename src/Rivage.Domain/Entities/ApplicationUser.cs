using Microsoft.AspNetCore.Identity;

namespace Rivage.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public TrainerProfile? TrainerProfile { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
