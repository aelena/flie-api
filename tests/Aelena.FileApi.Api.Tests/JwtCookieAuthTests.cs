using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aelena.FileApi.Api.Auth;
using Aelena.FileApi.Api.Configuration;
using Aelena.FileApi.Core.Errors;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Aelena.FileApi.Api.Tests;

/// <summary>
/// Token validation and cookie issuance.
/// </summary>
/// <remarks>
/// This had no tests at all, despite being the only thing standing between a
/// request and an identity.
/// </remarks>
public class JwtCookieAuthTests
{
    private const string Secret = "a-test-secret-that-is-long-enough-for-hmac-sha256";

    private static AppSettings Settings(string? secret = null, string algorithm = "HS256") => new()
    {
        JwtSecretKey = secret ?? Secret,
        JwtAlgorithm = algorithm,
        JwtExpirationDays = 7
    };

    private static string Token(
        string? secret = null,
        string algorithm = SecurityAlgorithms.HmacSha256,
        string? userId = "u-1",
        string? email = "someone@example.com",
        TimeSpan? lifetime = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? Secret));
        var claims = new List<Claim>();
        if (userId is not null) claims.Add(new Claim("user_id", userId));
        if (email is not null) claims.Add(new Claim("email", email));

        var now = DateTime.UtcNow;
        var expires = now.Add(lifetime ?? TimeSpan.FromHours(1));
        var token = new JwtSecurityToken(
            claims: claims,
            // notBefore must precede expires even when minting a deliberately stale token.
            notBefore: expires.AddHours(-1),
            expires: expires,
            signingCredentials: new SigningCredentials(key, algorithm));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static DefaultHttpContext ContextWithCookie(string? token)
    {
        var ctx = new DefaultHttpContext();
        if (token is not null)
            ctx.Request.Headers.Cookie = $"{JwtCookieAuth.CookieName}={token}";
        return ctx;
    }

    // ── Reading the cookie ───────────────────────────────────────────────

    [Fact]
    public void GetUserFromCookie_ValidToken_ReturnsUser()
    {
        var user = JwtCookieAuth.GetUserFromCookie(ContextWithCookie(Token()).Request, Settings());

        user.Should().NotBeNull();
        user!.UserId.Should().Be("u-1");
        user.Email.Should().Be("someone@example.com");
    }

    [Fact]
    public void GetUserFromCookie_NoCookie_ReturnsNull() =>
        JwtCookieAuth.GetUserFromCookie(ContextWithCookie(null).Request, Settings())
            .Should().BeNull();

    [Fact]
    public void GetUserFromCookie_Garbage_ReturnsNull() =>
        JwtCookieAuth.GetUserFromCookie(ContextWithCookie("not-a-jwt").Request, Settings())
            .Should().BeNull();

    [Fact]
    public void GetUserFromCookie_SignedWithAnotherSecret_ReturnsNull() =>
        JwtCookieAuth.GetUserFromCookie(
                ContextWithCookie(Token(secret: "a-completely-different-secret-value-here")).Request,
                Settings())
            .Should().BeNull();

    [Fact]
    public void GetUserFromCookie_Expired_ReturnsNull()
    {
        // Beyond the one-minute clock skew the validator allows.
        var expired = Token(lifetime: TimeSpan.FromMinutes(-30));

        JwtCookieAuth.GetUserFromCookie(ContextWithCookie(expired).Request, Settings())
            .Should().BeNull();
    }

    [Theory]
    [InlineData(null, "someone@example.com")]
    [InlineData("u-1", null)]
    public void GetUserFromCookie_MissingClaims_ReturnsNull(string? userId, string? email) =>
        JwtCookieAuth.GetUserFromCookie(
                ContextWithCookie(Token(userId: userId, email: email)).Request, Settings())
            .Should().BeNull();

    [Fact]
    public void GetUserFromCookie_UnexpectedAlgorithm_ReturnsNull()
    {
        // The configured algorithm is HS256; a token declaring HS512 must be refused
        // even though it is signed with the very same secret. Validation used to accept
        // whatever "alg" the token itself carried.
        var longEnoughForHs512 = new string('k', 80);
        var otherAlgorithm = Token(secret: longEnoughForHs512, algorithm: SecurityAlgorithms.HmacSha512);

        JwtCookieAuth.GetUserFromCookie(
                ContextWithCookie(otherAlgorithm).Request, Settings(secret: longEnoughForHs512))
            .Should().BeNull();
    }

    // ── Issuing the cookie ───────────────────────────────────────────────

    [Fact]
    public void SetCookie_ValidBearer_IssuesHttpOnlyCookieAndReturnsEmail()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = $"Bearer {Token()}";

        var email = JwtCookieAuth.SetCookie(ctx.Request, ctx.Response, Settings());

        email.Should().Be("someone@example.com");

        var setCookie = ctx.Response.Headers.SetCookie.ToString();
        setCookie.Should().Contain(JwtCookieAuth.CookieName)
            .And.Contain("httponly", AtLeast.Once())
            .And.Contain("secure", AtLeast.Once());
    }

    [Fact]
    public void SetCookie_NoAuthorizationHeader_IsUnauthorized() =>
        FluentActions.Invoking(() =>
            {
                var ctx = new DefaultHttpContext();
                return JwtCookieAuth.SetCookie(ctx.Request, ctx.Response, Settings());
            })
            .Should().Throw<FileApiException>()
            .Which.StatusCode.Should().Be(401);

    [Fact]
    public void SetCookie_ExpiredToken_SaysSo()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = $"Bearer {Token(lifetime: TimeSpan.FromMinutes(-30))}";

        FluentActions.Invoking(() => JwtCookieAuth.SetCookie(ctx.Request, ctx.Response, Settings()))
            .Should().Throw<FileApiException>()
            .Which.Detail.Should().Be("Token expired");
    }

    [Fact]
    public void SetCookie_ForgedSignature_IsUnauthorized()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Authorization = $"Bearer {Token(secret: "an-attacker-chosen-secret-of-some-length")}";

        FluentActions.Invoking(() => JwtCookieAuth.SetCookie(ctx.Request, ctx.Response, Settings()))
            .Should().Throw<FileApiException>()
            .Which.Detail.Should().Be("Invalid token");
    }

    [Fact]
    public void SetCookie_ReissuedTokenIsAcceptedBack()
    {
        var issuing = new DefaultHttpContext();
        issuing.Request.Headers.Authorization = $"Bearer {Token()}";
        JwtCookieAuth.SetCookie(issuing.Request, issuing.Response, Settings());

        var cookie = issuing.Response.Headers.SetCookie.ToString();
        var value = cookie[(cookie.IndexOf('=') + 1)..cookie.IndexOf(';')];

        JwtCookieAuth.GetUserFromCookie(ContextWithCookie(value).Request, Settings())
            .Should().NotBeNull();
    }

    // ── Startup validation ───────────────────────────────────────────────

    [Fact]
    public void ThrowIfUnsafeForProduction_PlaceholderSecretInProduction_Throws() =>
        FluentActions.Invoking(() =>
                Settings(secret: AppSettings.PlaceholderJwtSecret).ThrowIfUnsafeForProduction(isDevelopment: false))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*placeholder*");

    [Fact]
    public void ThrowIfUnsafeForProduction_PlaceholderSecretInDevelopment_IsAllowed() =>
        FluentActions.Invoking(() =>
                Settings(secret: AppSettings.PlaceholderJwtSecret).ThrowIfUnsafeForProduction(isDevelopment: true))
            .Should().NotThrow();

    [Fact]
    public void ThrowIfUnsafeForProduction_ShortSecretInProduction_Throws() =>
        FluentActions.Invoking(() => Settings(secret: "too-short").ThrowIfUnsafeForProduction(isDevelopment: false))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 32 bytes*");

    [Fact]
    public void ThrowIfUnsafeForProduction_UnsupportedAlgorithm_ThrowsEvenInDevelopment() =>
        FluentActions.Invoking(() => Settings(algorithm: "RS256").ThrowIfUnsafeForProduction(isDevelopment: true))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*not supported*");

    [Fact]
    public void ThrowIfUnsafeForProduction_ProperSecret_IsAllowed() =>
        FluentActions.Invoking(() => Settings().ThrowIfUnsafeForProduction(isDevelopment: false))
            .Should().NotThrow();
}
