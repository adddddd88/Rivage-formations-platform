namespace Rivage.Domain.Interfaces;

public record AiAvatarSessionRequest(
    string ModuleTitle,
    string ModuleContent,
    string? LearnerName = null,
    string? FormationTitle = null);

public record AiAvatarSessionResult(
    bool IsAvailable,
    bool IsMock,
    string? SessionToken,
    string ProviderName,
    string Message,
    string? NarrationScript = null);

public record AiAvatarAskRequest(
    string Question,
    string ModuleTitle,
    string ModuleContent);

public record AiAvatarAskResult(
    bool Success,
    string Answer,
    bool IsMock);

public interface IAiAvatarService
{
    bool IsConfigured { get; }
    string ProviderName { get; }
    Task<AiAvatarSessionResult> CreateSessionAsync(AiAvatarSessionRequest request, CancellationToken cancellationToken = default);
    Task<AiAvatarAskResult> AskAsync(AiAvatarAskRequest request, CancellationToken cancellationToken = default);
}