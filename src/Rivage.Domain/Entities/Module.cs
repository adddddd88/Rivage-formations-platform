using Rivage.Domain.Enums;

namespace Rivage.Domain.Entities;

public class Module
{
    public int Id { get; set; }
    public int FormationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public ModuleContentType ContentType { get; set; } = ModuleContentType.Lesson;
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Formation Formation { get; set; } = null!;
    public Exercise? Exercise { get; set; }
    public Quiz? Quiz { get; set; }
    public ICollection<ModuleProgress> ProgressRecords { get; set; } = [];
}
