namespace Rivage.Domain.Entities;

public class TrainerProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<Formation> Formations { get; set; } = [];
}
