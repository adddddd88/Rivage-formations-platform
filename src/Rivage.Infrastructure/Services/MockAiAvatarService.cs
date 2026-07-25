using Rivage.Domain.Interfaces;

namespace Rivage.Infrastructure.Services;

/// <summary>
/// Fallback avatar when Anam.ai API key is missing or the remote call fails.
/// Client uses browser SpeechSynthesis for oral presentation.
/// </summary>
public class MockAiAvatarService : IAiAvatarService
{
    public bool IsConfigured => false;
    public string ProviderName => "Mock (navigateur TTS)";

    public Task<AiAvatarSessionResult> CreateSessionAsync(AiAvatarSessionRequest request, CancellationToken cancellationToken = default)
    {
        var script = BuildNarration(request);
        return Task.FromResult(new AiAvatarSessionResult(
            IsAvailable: true,
            IsMock: true,
            SessionToken: null,
            ProviderName: ProviderName,
            Message: "Mode démonstration : narration orale via le navigateur (SpeechSynthesis).",
            NarrationScript: script));
    }

    public Task<AiAvatarAskResult> AskAsync(AiAvatarAskRequest request, CancellationToken cancellationToken = default)
    {
        var excerpt = request.ModuleContent.Length > 280
            ? request.ModuleContent[..280] + "…"
            : request.ModuleContent;

        var answer =
            $"Bonne question. Dans le module « {request.ModuleTitle} », retenez ceci : {excerpt} " +
            $"Concernant « {request.Question} », relisez le contenu du module et reformulez l'idée principale avec vos propres mots. " +
            "Si un point reste flou, avancez étape par étape puis validez avec le quiz.";

        return Task.FromResult(new AiAvatarAskResult(true, answer, IsMock: true));
    }

    private static string BuildNarration(AiAvatarSessionRequest request)
    {
        var greeting = string.IsNullOrWhiteSpace(request.LearnerName)
            ? "Bonjour"
            : $"Bonjour {request.LearnerName}";

        var formation = string.IsNullOrWhiteSpace(request.FormationTitle)
            ? "cette formation"
            : $"la formation {request.FormationTitle}";

        var body = request.ModuleContent.Length > 600
            ? request.ModuleContent[..600] + "…"
            : request.ModuleContent;

        return $"{greeting}. Bienvenue sur Rivage, votre guide sur {formation}. " +
               $"Nous abordons maintenant le module « {request.ModuleTitle} ». {body} " +
               "Prenez le temps d'écouter, puis posez-moi vos questions à l'oral ou à l'écrit.";
    }
}
