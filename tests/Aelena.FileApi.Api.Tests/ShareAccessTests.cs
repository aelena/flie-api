using System.Net;
using System.Net.Http.Json;
using Aelena.FileApi.Core.Models;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aelena.FileApi.Api.Tests;

/// <summary>
/// Access control on share links.
/// </summary>
/// <remarks>
/// A share's expiry and password were recorded when the link was created and then
/// never consulted when it was opened: <c>GET /share/{token}</c> looked the row up,
/// bumped the access counter, and returned the report. An expired link kept working
/// forever, and a password-protected link opened for anyone holding the URL — the
/// password was write-only.
/// </remarks>
public class ShareAccessTests(WebApplicationFactory<Program> factory) : FileApiFixture(factory)
{
    private async Task<string> CreateShare(object request)
    {
        var response = await Client.PostAsJsonAsync("/share", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var created = await response.Content.ReadFromJsonAsync<CreateShareResponse>();
        return created!.Token;
    }

    private static object ShareRequest(
        string? password = null, string? expiresAt = null,
        string accessType = "anyone", string[]? allowedEmails = null) => new
        {
            report = new { jobId = "job-1", status = "complete" },
            accessType,
            allowedEmails,
            password,
            expiresAt
        };

    [Fact]
    public async Task OpenShare_NoRestrictions_ReturnsTheReport()
    {
        var token = await CreateShare(ShareRequest());

        var response = await Client.GetAsync($"/share/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenShare_Expired_IsGone()
    {
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
        var token = await CreateShare(ShareRequest(expiresAt: yesterday));

        var response = await Client.GetAsync($"/share/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task OpenShare_NotYetExpired_Succeeds()
    {
        var tomorrow = DateTimeOffset.UtcNow.AddDays(1).ToString("o");
        var token = await CreateShare(ShareRequest(expiresAt: tomorrow));

        var response = await Client.GetAsync($"/share/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenShare_PasswordProtected_WithoutPassword_IsUnauthorized()
    {
        var token = await CreateShare(ShareRequest(password: "correct horse"));

        var response = await Client.GetAsync($"/share/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenShare_PasswordProtected_WrongPassword_IsUnauthorized()
    {
        var token = await CreateShare(ShareRequest(password: "correct horse"));

        var response = await Client.GetAsync($"/share/{token}?password=wrong");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OpenShare_PasswordProtected_CorrectPassword_Succeeds()
    {
        var token = await CreateShare(ShareRequest(password: "correct horse"));

        var response = await Client.GetAsync($"/share/{token}?password={Uri.EscapeDataString("correct horse")}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenShare_RestrictedToNamedRecipients_IsForbiddenForAnonymous()
    {
        var token = await CreateShare(ShareRequest(
            accessType: "restricted",
            allowedEmails: ["someone@example.com"]));

        var response = await Client.GetAsync($"/share/{token}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OpenShare_UnknownToken_IsNotFound()
    {
        var response = await Client.GetAsync("/share/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefusedShare_DoesNotLeakTheReportBody()
    {
        var token = await CreateShare(ShareRequest(password: "correct horse"));

        var response = await Client.GetAsync($"/share/{token}");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("job-1");
    }
}
