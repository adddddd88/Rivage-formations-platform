using FluentAssertions;
using Rivage.Domain.Interfaces;
using Rivage.Infrastructure.Services;

namespace Rivage.Tests.Unit;

public class MockAiAvatarServiceTests
{
    private readonly MockAiAvatarService _sut = new();

    [Fact]
    public void IsConfigured_is_false()
    {
        _sut.IsConfigured.Should().BeFalse();
        _sut.ProviderName.Should().Contain("Mock");
    }

    [Fact]
    public async Task CreateSessionAsync_returns_mock_narration()
    {
        var result = await _sut.CreateSessionAsync(new AiAvatarSessionRequest(
            ModuleTitle: "Le rivage du problème",
            ModuleContent: "Comprendre le besoin avant la solution.",
            LearnerName: "Lina",
            FormationTitle: "Product Thinking"));

        result.IsAvailable.Should().BeTrue();
        result.IsMock.Should().BeTrue();
        result.SessionToken.Should().BeNull();
        result.NarrationScript.Should().Contain("Bonjour Lina");
        result.NarrationScript.Should().Contain("Product Thinking");
        result.NarrationScript.Should().Contain("Le rivage du problème");
        result.NarrationScript.Should().Contain("Comprendre le besoin");
    }

    [Fact]
    public async Task CreateSessionAsync_truncates_long_content()
    {
        var longContent = new string('a', 800);
        var result = await _sut.CreateSessionAsync(new AiAvatarSessionRequest(
            "Module", longContent));

        result.NarrationScript.Should().Contain("…");
        result.NarrationScript!.Length.Should().BeLessThan(longContent.Length + 200);
    }

    [Fact]
    public async Task AskAsync_returns_contextual_answer()
    {
        var result = await _sut.AskAsync(new AiAvatarAskRequest(
            Question: "Qu'est-ce qu'un KPI ?",
            ModuleTitle: "Indicateurs",
            ModuleContent: "Un indicateur utile éclaire une décision."));

        result.Success.Should().BeTrue();
        result.IsMock.Should().BeTrue();
        result.Answer.Should().Contain("Indicateurs");
        result.Answer.Should().Contain("Qu'est-ce qu'un KPI");
        result.Answer.Should().Contain("indicateur utile");
    }
}
