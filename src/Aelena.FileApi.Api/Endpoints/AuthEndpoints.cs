using Aelena.FileApi.Api.Auth;
using Aelena.FileApi.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Aelena.FileApi.Api.Endpoints;

/// <summary>Authentication endpoints for JWT cookie management.</summary>
public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/set-cookie", (HttpContext ctx, IOptions<AppSettings> settings) =>
        {
            var email = JwtCookieAuth.SetCookie(ctx.Request, ctx.Response, settings.Value);
            return Results.Ok(new { success = true, email });
        })
        .WithName("SetAuthCookie")
        .WithDescription("Receive a JWT via Authorization header and set it as an httpOnly cookie.")
        .Produces<object>(200)
        .Produces<ProblemDetail>(401);

        return group;
    }
}
