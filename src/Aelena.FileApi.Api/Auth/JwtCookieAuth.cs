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

    /// <summary>Represents an authenticated user extracted from a JWT.</summary>
    public sealed record UserInfo(string UserId, string Email);

    /// <summary>
    /// Extracts user info from the <c>auth_token</c> cookie.
    /// Returns <c>null</c> if the cookie is missing, expired, or malformed.
    /// </summary>
    public static UserInfo? GetUserFromCookie(HttpRequest request, AppSettings settings)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var token) || string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecretKey));
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            var userId = principal.FindFirstValue("user_id");
            var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);

            return userId is not null && email is not null
                ? new UserInfo(userId, email)
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates an incoming Bearer token, re-signs it, and sets it as an httpOnly cookie.
    /// Returns the user's email on success, or throws <see cref="FileApiException"/>.
    /// </summary>
    public static string SetCookie(HttpRequest request, HttpResponse response, AppSettings settings)
    {
        var authHeader = request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            throw new FileApiException(401, "Missing Bearer token");

        var token = authHeader["Bearer ".Length..].Trim();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecretKey));

        ClaimsPrincipal principal;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
        }
        catch (SecurityTokenExpiredException)
        {
            throw new FileApiException(401, "Token expired");
        }
        catch
        {
            throw new FileApiException(401, "Invalid token");
        }

        var userId = principal.FindFirstValue("user_id");
        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);

        if (userId is null || email is null)
            throw new FileApiException(401, "Invalid token: missing user data");

        // Re-sign with fresh expiration
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim("user_id", userId),
            new Claim("email", email)
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var newToken = new JwtSecurityToken(
            expires: now.AddDays(settings.JwtExpirationDays),
            claims: claims,
            signingCredentials: creds,
            notBefore: now);

        var encoded = new JwtSecurityTokenHandler().WriteToken(newToken);

        response.Cookies.Append(CookieName, encoded, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(settings.JwtExpirationDays),
            Path = "/"
        });

        return email;
    }
}
