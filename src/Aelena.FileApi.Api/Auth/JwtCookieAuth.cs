using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aelena.FileApi.Api.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Aelena.FileApi.Api.Auth;

/// <summary>
/// JWT cookie-based authentication helpers.
/// The frontend calls <c>POST /api/auth/set-cookie</c> with a Bearer token,
/// which is re-signed and stored as an httpOnly cookie for subsequent requests.
/// </summary>
public static class JwtCookieAuth
{
    public const string CookieName = "auth_token";

    private const string BearerPrefix = "Bearer ";

    /// <summary>Represents an authenticated user extracted from a JWT.</summary>
    public sealed record UserInfo(string UserId, string Email);

    /// <summary>
    /// Extracts user info from the <c>auth_token</c> cookie.
    /// Returns <c>null</c> if the cookie is missing, expired, or malformed.
    /// </summary>
    public static UserInfo? GetUserFromCookie(HttpRequest request, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(settings);

        if (!request.Cookies.TryGetValue(CookieName, out var token) || string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, ValidationParameters(settings), out _);

            return ReadUser(principal);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            // An unreadable cookie means "not signed in", not "server error".
            return null;
        }
    }

    /// <summary>
    /// Validates an incoming Bearer token, re-signs it, and sets it as an httpOnly cookie.
    /// Returns the user's email on success, or throws <see cref="FileApiException"/>.
    /// </summary>
    public static string SetCookie(HttpRequest request, HttpResponse response, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(settings);

        var authHeader = request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            throw new FileApiException(401, "Missing Bearer token");

        var token = authHeader[BearerPrefix.Length..].Trim();

        ClaimsPrincipal principal;
        try
        {
            principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, ValidationParameters(settings), out _);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new FileApiException(401, "Token expired");
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            throw new FileApiException(401, "Invalid token");
        }

        var user = ReadUser(principal)
            ?? throw new FileApiException(401, "Invalid token: missing user data");

        var now = DateTime.UtcNow;
        var key = SigningKey(settings);
        var reissued = new JwtSecurityToken(
            expires: now.AddDays(settings.JwtExpirationDays),
            claims: [new Claim("user_id", user.UserId), new Claim("email", user.Email)],
            signingCredentials: new SigningCredentials(key, settings.AllowedJwtAlgorithms[0]),
            notBefore: now);

        response.Cookies.Append(CookieName, new JwtSecurityTokenHandler().WriteToken(reissued), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(settings.JwtExpirationDays),
            Path = "/"
        });

        return user.Email;
    }

    private static UserInfo? ReadUser(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue("user_id");
        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);

        return userId is not null && email is not null ? new UserInfo(userId, email) : null;
    }

    private static SymmetricSecurityKey SigningKey(AppSettings settings) =>
        new(Encoding.UTF8.GetBytes(settings.JwtSecretKey));

    /// <summary>Validation rules shared by both entry points.</summary>
    /// <remarks>
    /// <c>ValidAlgorithms</c> pins the signature algorithm to the configured one.
    /// Without it the handler accepts whatever <c>alg</c> the token declares, and
    /// <see cref="AppSettings.JwtAlgorithm"/> — which exists to say which algorithm is
    /// in use — was read by nothing at all.
    /// </remarks>
    private static TokenValidationParameters ValidationParameters(AppSettings settings) => new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidAlgorithms = settings.AllowedJwtAlgorithms,
        IssuerSigningKey = SigningKey(settings),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
