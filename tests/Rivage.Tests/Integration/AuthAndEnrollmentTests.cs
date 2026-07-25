using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Rivage.Infrastructure.Data;

namespace Rivage.Tests.Integration;

public class AuthAndEnrollmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthAndEnrollmentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_creates_learner_and_redirects()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var email = $"learner-{Guid.NewGuid():N}@rivage.test";

        var get = await client.GetAsync("/Account/Register");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await get.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FirstName"] = "Nova",
            ["LastName"] = "Test",
            ["Email"] = email,
            ["Password"] = "Rivage@Test2026!",
            ["ConfirmPassword"] = "Rivage@Test2026!",
            ["ConfirmEmailValid"] = "true"
        };

        var post = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(form));
        post.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.SeeOther);
        post.Headers.Location!.ToString().Should().Contain("/Learner");
    }

    [Fact]
    public async Task Login_with_seeded_learner_succeeds()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        // Ensure host started (seed ran)
        _ = await client.GetAsync("/");

        var get = await client.GetAsync("/Account/Login");
        get.EnsureSuccessStatusCode();
        var html = await get.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = "apprenant@rivage.local",
            ["Password"] = "Rivage@Learner2026!",
            ["RememberMe"] = "false"
        };

        var post = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(form));
        post.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.SeeOther);
        post.Headers.Location!.ToString().Should().Contain("/Learner");
    }

    [Fact]
    public async Task Login_then_enroll_in_published_formation()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        // Login
        var loginPage = await client.GetAsync("/Account/Login");
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var loginToken = ExtractAntiforgeryToken(loginHtml);

        var loginPost = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = loginToken,
            ["Email"] = "apprenant@rivage.local",
            ["Password"] = "Rivage@Learner2026!",
            ["RememberMe"] = "false"
        }));
        loginPost.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.SeeOther);

        int formationId;
        string slug;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RivageDbContext>();
            var formation = db.Formations.First(f => f.IsPublished);
            formationId = formation.Id;
            slug = formation.Slug;
        }

        var details = await client.GetAsync($"/Catalog/Details?slug={Uri.EscapeDataString(slug)}");
        details.EnsureSuccessStatusCode();
        var detailsHtml = await details.Content.ReadAsStringAsync();
        var enrollToken = ExtractAntiforgeryToken(detailsHtml);

        var enrollPost = await client.PostAsync("/Learner/Enroll", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = enrollToken,
            ["formationId"] = formationId.ToString()
        }));

        enrollPost.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.SeeOther);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RivageDbContext>();
            db.Enrollments.Any(e => e.FormationId == formationId).Should().BeTrue();
        }
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                html,
                @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
                RegexOptions.IgnoreCase);
        }

        match.Success.Should().BeTrue("antiforgery token should be present in the form");
        return match.Groups[1].Value;
    }
}
