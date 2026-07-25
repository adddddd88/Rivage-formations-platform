using FluentAssertions;
using Rivage.Infrastructure.Services;

namespace Rivage.Tests.Unit;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Product Management", "product-management")]
    [InlineData("Découvrir le Product Thinking", "decouvrir-le-product-thinking")]
    [InlineData("  Hello   World  ", "hello-world")]
    [InlineData("Data & Analytique!", "data-analytique")]
    public void Generate_normalizes_title_to_slug(string input, string expected)
    {
        SlugHelper.Generate(input).Should().Be(expected);
    }

    [Fact]
    public void Generate_empty_returns_short_token()
    {
        var slug = SlugHelper.Generate("   ");
        slug.Should().HaveLength(8);
        slug.Should().MatchRegex("^[a-f0-9]{8}$");
    }

    [Fact]
    public void Generate_strips_accents()
    {
        SlugHelper.Generate("Écume & Brise").Should().Be("ecume-brise");
    }
}
