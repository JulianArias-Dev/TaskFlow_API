using TaskFlow_API.Services;

namespace TaskFlow_API.Middleware;

/// <summary>
/// Middleware que verifica si el JWT presente en el request fue revocado (logout).
/// Se ejecuta DESPUÉS de UseAuthentication para tener acceso al token validado.
/// </summary>
public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async System.Threading.Tasks.Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklist)
    {
        // Sólo aplica a requests autenticados (sin Authorization header sigue de largo).
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();

            if (await blacklist.IsRevokedAsync(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Token revoked. Please login again.\",\"statusCode\":401}");
                return;
            }
        }

        await _next(context);
    }
}

public static class JwtBlacklistMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtBlacklist(this IApplicationBuilder app)
        => app.UseMiddleware<JwtBlacklistMiddleware>();
}
