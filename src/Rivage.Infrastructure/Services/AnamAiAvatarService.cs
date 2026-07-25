using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rivage.Domain.Interfaces;

namespace Rivage.Infrastructure.Services;

public class AnamAiAvatarService : IAiAvatarService
{
    private readonly HttpClient _http;
    private readonly AnamOptions _options;
    private readonly MockAiAvatarService _fallback;
    private readonly ILogger<AnamAiAvatarService> _logger;

    public AnamAiAvatarService(
        HttpClient http,
        IOptions<AnamOptions> options,
        MockAiAvatarService fallback,
        ILogger<AnamAiAvatarService> logger)
    {
        _http = http;
        _options = options.Value;
        _fallback = fallback;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);
    public string ProviderName => IsConfigured ? "Anam.ai" : _fallback.ProviderName;

    public async Task<AiAvatarSessionResult> CreateSessionAsync(AiAvatarSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return await _fallback.CreateSessionAsync(request, cancellationToken);

        try
        {
            var systemPrompt =
                "Tu es le formateur IA de la plateforme Rivage. Tu présentes les modules à l'oral, " +
                "tu réponds clairement aux questions des apprenants, en français, avec un ton pédagogique et encourageant. " +
                $"Module : {request.ModuleTitle}. Contenu : {Truncate(request.ModuleContent, 3500)}";

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiBaseUrl.TrimEnd('/')}/v1/auth/session-token");
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            message.Content = JsonContent.Create(new
            {
                personaConfig = new
                {
                    name = "Formateur Rivage",
                    avatarId = _options.AvatarId,
                    avatarModel = _options.AvatarModel,
                    voiceId = _options.VoiceId,
                    llmId = _options.LlmId,
                    systemPrompt
                }
            });

            var response = await _http.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Anam session-token failed ({Status}): {Body}", response.StatusCode, body);
                var mock = await _fallback.CreateSessionAsync(request, cancellationToken);
                return mock with
                {
                    Message = $"Anam indisponible ({(int)response.StatusCode}) — bascule sur le mode démonstration. {mock.Message}"
                };
            }

            var payload = await response.Content.ReadFromJsonAsync<SessionTokenResponse>(cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(payload?.SessionToken))
            {
                return await _fallback.CreateSessionAsync(request, cancellationToken);
            }

            return new AiAvatarSessionResult(
                IsAvailable: true,
                IsMock: false,
                SessionToken: payload.SessionToken,
                ProviderName: ProviderName,
                Message: "Session Anam.ai prête.",
                NarrationScript: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anam session creation failed");
            var mock = await _fallback.CreateSessionAsync(request, cancellationToken);
            return mock with { Message = $"Erreur Anam — bascule démonstration. {mock.Message}" };
        }
    }

    public async Task<AiAvatarAskResult> AskAsync(AiAvatarAskRequest request, CancellationToken cancellationToken = default)
    {
        // Oral Q&A is handled by the Anam WebRTC persona when live.
        // For mock / text fallback we always provide a useful answer.
        return await _fallback.AskAsync(request, cancellationToken);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    private sealed class SessionTokenResponse
    {
        [JsonPropertyName("sessionToken")]
        public string? SessionToken { get; set; }
    }
}
