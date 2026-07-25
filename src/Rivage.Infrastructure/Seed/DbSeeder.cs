using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rivage.Domain.Entities;
using Rivage.Domain.Enums;
using Rivage.Infrastructure.Data;
using Rivage.Infrastructure.Services;

namespace Rivage.Infrastructure.Seed;

public class DbSeeder
{
    private readonly RivageDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IConfiguration _config;
    private readonly ILogger<DbSeeder> _logger;

    private static readonly string[] Levels = ["Débutant", "Intermédiaire", "Avancé"];

    public DbSeeder(
        RivageDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        IConfiguration config,
        ILogger<DbSeeder> logger)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _config = config;
        _logger = logger;
    }

    public async Task MigrateAndSeedAsync()
    {
        if (_db.Database.IsRelational())
            await _db.Database.MigrateAsync();
        else
            await _db.Database.EnsureCreatedAsync();

        await SeedRolesAsync();
        await SeedUsersAndContentAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await _roles.RoleExistsAsync(role))
                await _roles.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task SeedUsersAndContentAsync()
    {
        if (await _db.Categories.AnyAsync())
        {
            _logger.LogInformation("Seed skipped — data already present. Reset DB volume for a fresh seed.");
            return;
        }

        var adminEmail = _config["SEED_ADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL") ?? "admin@rivage.local";
        var adminPassword = _config["SEED_ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "Rivage@Admin2026!";

        await EnsureUserAsync(adminEmail, adminPassword, "Amina", "Marée", AppRoles.Admin);

        var trainerSpecs = new (string Email, string First, string Last, string Specialty, string Bio)[]
        {
            ("formateur@rivage.local", "Youssef", "Écume", "Product & Leadership", "Formateur senior en product management."),
            ("formateur2@rivage.local", "Sara", "Horizon", "Data & Analytique", "Spécialiste data literacy et visualisation."),
            ("formateur3@rivage.local", "Karim", "Phare", "Développement web", "Ingénieur full-stack et mentor technique."),
            ("formateur4@rivage.local", "Nadia", "Brise", "UX & Design", "Designer produit et facilitation d'ateliers."),
            ("formateur5@rivage.local", "Inès", "Largue", "Cloud & DevOps", "Architecte cloud et pratiques CI/CD."),
            ("formateur6@rivage.local", "Mehdi", "Rivage", "Soft skills", "Coach en communication et management."),
        };

        var trainerProfiles = new List<TrainerProfile>();
        foreach (var t in trainerSpecs)
        {
            var user = await EnsureUserAsync(t.Email, "Rivage@Trainer2026!", t.First, t.Last, AppRoles.Trainer);
            var profile = new TrainerProfile
            {
                UserId = user.Id,
                Specialty = t.Specialty,
                Bio = t.Bio,
                IsActive = true
            };
            trainerProfiles.Add(profile);
        }

        _db.TrainerProfiles.AddRange(trainerProfiles);

        var learnerSpecs = new (string Email, string First, string Last)[]
        {
            ("apprenant@rivage.local", "Lina", "Vague"),
            ("apprenant2@rivage.local", "Omar", "Brise"),
            ("apprenant3@rivage.local", "Salma", "Écume"),
            ("apprenant4@rivage.local", "Yassine", "Port"),
            ("apprenant5@rivage.local", "Hana", "Cale"),
            ("apprenant6@rivage.local", "Bilal", "Ancre"),
            ("apprenant7@rivage.local", "Rania", "Marée"),
            ("apprenant8@rivage.local", "Adil", "Horizon"),
        };

        var learners = new List<ApplicationUser>();
        foreach (var l in learnerSpecs)
            learners.Add(await EnsureUserAsync(l.Email, "Rivage@Learner2026!", l.First, l.Last, AppRoles.Learner));

        // 22 parcours (catégories)
        var parcoursDefs = new (string Name, string Description)[]
        {
            ("Product Management", "Cadrer, prioriser et livrer de la valeur produit."),
            ("Data & Analytique", "Lire les données et décider avec méthode."),
            ("Développement Web", "Front, back et bonnes pratiques modernes."),
            ("UX & Design", "Recherche utilisateur, parcours et interfaces."),
            ("Cloud & DevOps", "Déployer, automatiser et observer les systèmes."),
            ("Cybersécurité", "Sécuriser applications et bonnes pratiques."),
            ("Intelligence Artificielle", "Bases IA, prompts et cas d'usage métier."),
            ("Soft Skills", "Communication, feedback et collaboration."),
            ("Management d'équipe", "Animer, déléguer et faire grandir une équipe."),
            ("Marketing digital", "Acquisition, contenu et mesure de performance."),
            ("Finance pour non-financiers", "Lire un budget et piloter la rentabilité."),
            ("Agilité & Scrum", "Rituels, rôles et livraison itérative."),
            ("Qualité logicielle", "Tests, revue de code et dette technique."),
            ("Bases de données", "Modélisation SQL et requêtes efficaces."),
            ("Mobile", "Concevoir et livrer des apps mobiles."),
            ("No-code / Low-code", "Prototyper et automatiser sans tout coder."),
            ("Entrepreneuriat", "Valider une idée et construire une offre."),
            ("Gestion de projet", "Planifier, suivre et livrer dans les délais."),
            ("Support & Customer Success", "Accompagner et fidéliser les clients."),
            ("Data Engineering", "Pipelines, qualité et préparation des données."),
            ("Architecture logicielle", "Découper, découpler et faire évoluer un système."),
            ("Accessibilité numérique", "Concevoir pour tous les utilisateurs."),
        };

        var categories = parcoursDefs.Select(p => new Category
        {
            Name = p.Name,
            Slug = SlugHelper.Generate(p.Name),
            Description = p.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _db.Categories.AddRange(categories);
        await _db.SaveChangesAsync();

        // 42 formations réparties sur les parcours
        var formationTitles = BuildFormationCatalog();
        var formations = new List<Formation>();
        var rng = new Random(42);

        for (var i = 0; i < formationTitles.Count; i++)
        {
            var title = formationTitles[i];
            var category = categories[i % categories.Count];
            var trainer = trainerProfiles[i % trainerProfiles.Count];
            var level = Levels[i % Levels.Length];
            var hours = 4 + (i % 7) * 2;

            formations.Add(new Formation
            {
                Title = title,
                Slug = UniqueSlug(SlugHelper.Generate(title), i),
                ShortDescription = $"Parcours « {category.Name} » — {title}. Formation pratique guidée sur Rivage.",
                Description =
                    $"{title} fait partie du parcours {category.Name}. " +
                    "Vous progresserez module par module : leçons, exercices appliqués et quiz de validation. " +
                    "Le formateur IA Rivage peut présenter chaque module à l'oral et répondre à vos questions. " +
                    $"Niveau {level}, durée estimée {hours} heures.",
                CategoryId = category.Id,
                TrainerProfileId = trainer.Id,
                DurationHours = hours,
                Level = level,
                IsPublished = i % 11 != 10, // quelques brouillons
                CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(1, 120))
            });
        }

        _db.Formations.AddRange(formations);
        await _db.SaveChangesAsync();

        var modules = new List<Module>();
        var exercises = new List<Exercise>();
        var quizzes = new List<Quiz>();

        foreach (var formation in formations)
        {
            var moduleCount = 4 + (formation.Id % 3); // 4 à 6 modules
            for (var order = 1; order <= moduleCount; order++)
            {
                var type = order switch
                {
                    2 => ModuleContentType.Exercise,
                    var o when o == moduleCount || o == 3 => ModuleContentType.Quiz,
                    _ => ModuleContentType.Lesson
                };

                // éviter deux quiz consécutifs trop tôt
                if (order == moduleCount - 1 && type == ModuleContentType.Quiz)
                    type = ModuleContentType.Lesson;

                var module = new Module
                {
                    FormationId = formation.Id,
                    Title = type switch
                    {
                        ModuleContentType.Exercise => $"Exercice : mise en pratique — {formation.Title}",
                        ModuleContentType.Quiz => $"Quiz : valider « {ShortTitle(formation.Title)} »",
                        _ => $"Module {order} — {LessonTitle(formation.Title, order)}"
                    },
                    Summary = type switch
                    {
                        ModuleContentType.Exercise => "Appliquez les concepts sur un cas concret.",
                        ModuleContentType.Quiz => "Évaluez vos acquis (seuil 70%).",
                        _ => "Concepts clés, exemples et points de vigilance."
                    },
                    Content = BuildModuleContent(formation, order, type),
                    OrderIndex = order,
                    EstimatedMinutes = type switch
                    {
                        ModuleContentType.Quiz => 12 + (order % 3) * 3,
                        ModuleContentType.Exercise => 20 + (order % 4) * 5,
                        _ => 18 + (order % 5) * 4
                    },
                    ContentType = type,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow
                };
                modules.Add(module);
            }
        }

        _db.Modules.AddRange(modules);
        await _db.SaveChangesAsync();

        foreach (var module in modules.Where(m => m.ContentType == ModuleContentType.Exercise))
        {
            exercises.Add(new Exercise
            {
                ModuleId = module.Id,
                Instructions =
                    "Rédigez une réponse structurée (8–15 lignes) : contexte, actions proposées, critères de succès, et un risque à anticiper. " +
                    $"Sujet lié à : {module.Title}.",
                SolutionHint = "Pensez impact utilisateur, faisabilité, et mesure du résultat.",
                MaxScore = 100
            });
        }

        foreach (var module in modules.Where(m => m.ContentType == ModuleContentType.Quiz))
        {
            quizzes.Add(BuildQuiz(module));
        }

        _db.Exercises.AddRange(exercises);
        _db.Quizzes.AddRange(quizzes);
        await _db.SaveChangesAsync();

        // quelques inscriptions démo
        var published = formations.Where(f => f.IsPublished).Take(12).ToList();
        var enrollments = new List<Enrollment>();
        for (var i = 0; i < learners.Count; i++)
        {
            foreach (var f in published.Skip(i).Take(3))
            {
                enrollments.Add(new Enrollment
                {
                    UserId = learners[i].Id,
                    FormationId = f.Id,
                    EnrolledAt = DateTime.UtcNow.AddDays(-(i + 1)),
                    Status = EnrollmentStatus.Active,
                    ProgressPercent = (i * 7) % 40
                });
            }
        }

        _db.Enrollments.AddRange(enrollments);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Seed completed: {Categories} parcours, {Formations} formations, {Modules} modules, {Quizzes} quiz, {Trainers} formateurs, {Learners} apprenants. Admin={Admin}",
            categories.Count, formations.Count, modules.Count, quizzes.Count, trainerProfiles.Count, learners.Count, adminEmail);
    }

    private static List<string> BuildFormationCatalog()
    {
        return
        [
            "Découvrir le Product Thinking",
            "Du besoin utilisateur à la roadmap",
            "Priorisation impact / effort",
            "Discovery continue en équipe",
            "OKR et pilotage produit",
            "Lire les données pour décider",
            "Tableaux de bord actionnables",
            "SQL pour analystes métier",
            "Storytelling avec les données",
            "Éviter les pièges statistiques",
            "HTML & CSS modernes",
            "JavaScript essentiel",
            "API REST bien conçues",
            "ASP.NET Core fondamentaux",
            "Git collaboratif et revues",
            "Recherche utilisateur express",
            "Wireframes et prototypage",
            "Design system pratique",
            "Accessibilité WCAG en 1 journée",
            "Tests utilisateurs à petit budget",
            "Docker pour développeurs",
            "CI/CD avec pipelines simples",
            "Observabilité : logs et métriques",
            "Déployer sur le cloud",
            "Infrastructure as Code découverte",
            "Sécurité applicative de base",
            "Gestion des secrets et accès",
            "RGPD pour équipes produit",
            "Prompt engineering utile",
            "IA générative au service du métier",
            "Communiquer avec impact",
            "Feedback constructif",
            "Animer une réunion efficace",
            "Leadership situationnel",
            "Déléguer sans perdre le fil",
            "Scrum en pratique",
            "Kanban et flux tiré",
            "Estimer et planifier sans magie",
            "Tests automatisés utiles",
            "Modélisation relationnelle",
            "Automatiser avec le no-code",
            "Pitcher une idée et valider le marché"
        ];
    }

    private static string UniqueSlug(string slug, int index) =>
        string.IsNullOrWhiteSpace(slug) ? $"formation-{index + 1}" : $"{slug}-{index + 1}";

    private static string ShortTitle(string title) =>
        title.Length <= 42 ? title : title[..42] + "…";

    private static string LessonTitle(string formationTitle, int order) =>
        order switch
        {
            1 => $"Les fondations de « {ShortTitle(formationTitle)} »",
            4 => "Aller plus loin et cas avancés",
            5 => "Bonnes pratiques et anti-patterns",
            _ => "Approfondissement et exemples terrain"
        };

    private static string BuildModuleContent(Formation formation, int order, ModuleContentType type)
    {
        if (type == ModuleContentType.Quiz)
        {
            return $"Ce quiz valide les notions du parcours {formation.Title}. " +
                   "Prenez le temps de lire chaque question. Score de réussite : 70%.";
        }

        if (type == ModuleContentType.Exercise)
        {
            return $"Mettez en pratique les idées du module précédent sur un cas lié à « {formation.Title} ». " +
                   "Documentez votre raisonnement : hypothèses, options, décision, prochaine étape.";
        }

        return
            $"Bienvenue dans le module {order} de la formation « {formation.Title} » (niveau {formation.Level}). " +
            "Objectifs : comprendre le concept central, l'illustrer avec un exemple concret, et identifier une action applicable dès demain. " +
            "Sur Rivage, avancez dans l'ordre des modules : chaque étape prépare la suivante. " +
            "Retenez trois idées : (1) clarifier le problème avant la solution, (2) mesurer ce qui compte vraiment, " +
            "(3) itérer avec des retours réels. Utilisez le formateur IA pour une présentation orale ou pour poser vos questions.";
    }

    private static Quiz BuildQuiz(Module module)
    {
        var topic = ShortTitle(module.Title.Replace("Quiz : valider « ", "").Replace(" »", ""));
        return new Quiz
        {
            ModuleId = module.Id,
            Title = $"Évaluation — {topic}",
            Instructions = "Choisissez la meilleure réponse. Seuil de réussite : 70%.",
            PassingScorePercent = 70,
            ShowResultsImmediately = true,
            Questions =
            [
                new Question
                {
                    Text = $"Dans le contexte de « {topic} », par quoi vaut-il mieux commencer ?",
                    Type = QuestionType.MultipleChoice,
                    Points = 1,
                    OrderIndex = 1,
                    Options =
                    [
                        new AnswerOption { Text = "Une solution technique détaillée", IsCorrect = false, OrderIndex = 1 },
                        new AnswerOption { Text = "Le problème / besoin et son contexte", IsCorrect = true, OrderIndex = 2 },
                        new AnswerOption { Text = "Le choix des outils uniquement", IsCorrect = false, OrderIndex = 3 },
                        new AnswerOption { Text = "La communication marketing", IsCorrect = false, OrderIndex = 4 }
                    ]
                },
                new Question
                {
                    Text = "Mesurer un indicateur sans lien avec une décision est souvent peu utile.",
                    Type = QuestionType.TrueFalse,
                    Points = 1,
                    OrderIndex = 2,
                    Options =
                    [
                        new AnswerOption { Text = "Vrai", IsCorrect = true, OrderIndex = 1 },
                        new AnswerOption { Text = "Faux", IsCorrect = false, OrderIndex = 2 }
                    ]
                },
                new Question
                {
                    Text = "Quelle attitude favorise le mieux l'apprentissage progressif ?",
                    Type = QuestionType.MultipleChoice,
                    Points = 1,
                    OrderIndex = 3,
                    Options =
                    [
                        new AnswerOption { Text = "Tout livrer d'un coup sans feedback", IsCorrect = false, OrderIndex = 1 },
                        new AnswerOption { Text = "Itérer, tester, ajuster à partir de retours", IsCorrect = true, OrderIndex = 2 },
                        new AnswerOption { Text = "Ignorer les utilisateurs", IsCorrect = false, OrderIndex = 3 }
                    ]
                },
                new Question
                {
                    Text = "Documenter hypothèses et critères de succès aide à décider plus clairement.",
                    Type = QuestionType.TrueFalse,
                    Points = 1,
                    OrderIndex = 4,
                    Options =
                    [
                        new AnswerOption { Text = "Vrai", IsCorrect = true, OrderIndex = 1 },
                        new AnswerOption { Text = "Faux", IsCorrect = false, OrderIndex = 2 }
                    ]
                }
            ]
        };
    }

    private async Task<ApplicationUser> EnsureUserAsync(string email, string password, string first, string last, string role)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            var result = await _users.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Cannot create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await _users.IsInRoleAsync(user, role))
            await _users.AddToRoleAsync(user, role);

        return user;
    }
}
