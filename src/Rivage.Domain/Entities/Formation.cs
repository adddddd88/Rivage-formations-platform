namespace Rivage.Domain.Entities;

public class Formation
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? TrainerProfileId { get; set; }
    public int DurationHours { get; set; }
    public string Level { get; set; } = "Débutant";
    public bool IsPublished { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public TrainerProfile? TrainerProfile { get; set; }
    public ICollection<Module> Modules { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
