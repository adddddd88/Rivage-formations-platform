using System.ComponentModel.DataAnnotations;
using Rivage.Domain.Enums;

namespace Rivage.Web.ViewModels;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class CategoryEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120), Display(Name = "Nom")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500), Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class FormationEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(400), Display(Name = "Résumé")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required, Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Catégorie")]
    public int CategoryId { get; set; }

    [Display(Name = "Formateur")]
    public int? TrainerProfileId { get; set; }

    [Display(Name = "Durée (heures)")]
    public int DurationHours { get; set; } = 4;

    [Display(Name = "Niveau")]
    public string Level { get; set; } = "Débutant";

    [Display(Name = "Publiée")]
    public bool IsPublished { get; set; }
}

public class ModuleEditViewModel
{
    public int Id { get; set; }
    public int FormationId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(400)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Ordre")]
    public int OrderIndex { get; set; } = 1;

    [Display(Name = "Minutes estimées")]
    public int EstimatedMinutes { get; set; } = 30;

    [Display(Name = "Type")]
    public ModuleContentType ContentType { get; set; } = ModuleContentType.Lesson;

    [Display(Name = "Publié")]
    public bool IsPublished { get; set; } = true;

    // Exercise fields
    public string? ExerciseInstructions { get; set; }
    public string? ExerciseHint { get; set; }

    // Quiz fields
    public string? QuizTitle { get; set; }
    public int PassingScorePercent { get; set; } = 70;
}

public class TrainerEditViewModel
{
    public int Id { get; set; }
    public string? UserId { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Display(Name = "Prénom")]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Nom")]
    public string LastName { get; set; } = string.Empty;

    [Required, Display(Name = "Spécialité")]
    public string Specialty { get; set; } = string.Empty;

    [Display(Name = "Bio")]
    public string Bio { get; set; } = string.Empty;

    [DataType(DataType.Password), Display(Name = "Mot de passe (création)")]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}

public class QuizQuestionEditViewModel
{
    public int Id { get; set; }
    public int QuizId { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; } = 1;

    public List<OptionEdit> Options { get; set; } =
    [
        new(), new(), new(), new()
    ];

    public class OptionEdit
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
