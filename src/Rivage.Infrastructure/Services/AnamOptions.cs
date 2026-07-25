namespace Rivage.Infrastructure.Services;

public class AnamOptions
{
    public const string SectionName = "Anam";

    public string? ApiKey { get; set; }
    public string ApiBaseUrl { get; set; } = "https://api.anam.ai";
    public string AvatarId { get; set; } = "30fa96d0-26c4-4e55-94a0-517025942e18";
    public string AvatarModel { get; set; } = "cara-4";
    public string VoiceId { get; set; } = "6bfbe25a-979d-40f3-a92b-5394170af54b";
    public string LlmId { get; set; } = "a7cf662c-2ace-4de1-a21e-ef0fbf144bb7";
}
