using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aelena.FileApi.Api.Tests;

[Collection("FileApi")]
public class HealthEndpointTests(WebApplicationFactory<Program> factory) : FileApiFixture(factory)
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerRedirect_ReturnsRedirectFromRoot()
    {
        var response = await Client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("swagger");
    }
}
