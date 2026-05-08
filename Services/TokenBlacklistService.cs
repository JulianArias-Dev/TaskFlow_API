using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Servicio para gestionar la blacklist de tokens JWT.
/// Combina caché en memoria (lookup O(1) sin tocar BD en cada request) +
/// persistencia para sobrevivir reinicios.
/// </summary>
public interface ITokenBlacklistService
{
    System.Threading.Tasks.Task RevokeAsync(string token, Guid userId, string? reason = null);
    System.Threading.Tasks.Task<bool> IsRevokedAsync(string token);
    System.Threading.Tasks.Task LoadFromDatabaseAsync();
}

public class TokenBlacklistService : ITokenBlacklistService
{
    // Caché en memoria — singleton — evita ir a BD por cada request
    private static readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenBlacklistService> _logger;

    public TokenBlacklistService(IServiceScopeFactory scopeFactory, ILogger<TokenBlacklistService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task RevokeAsync(string token, Guid userId, string? reason = null)
    {
        var (jti, expiresAt) = ExtractJtiAndExpiry(token);
        if (string.IsNullOrEmpty(jti)) return;

        // 1) Caché — efecto inmediato
        _blacklist[jti] = expiresAt;

        // 2) Persistencia — sobrevive a reinicios
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var revoked = new RevokedToken
        {
            TokenId = jti,
            UserId = userId,
            ExpiresAt = expiresAt,
            Reason = reason ?? "User logout"
        };

        await uow.RevokedTokens.AddAsync(revoked);
        await uow.SaveChangesAsync();

        _logger.LogInformation("Token revoked for user {UserId} (jti: {Jti})", userId, jti);
    }

    public async System.Threading.Tasks.Task<bool> IsRevokedAsync(string token)
    {
        var (jti, _) = ExtractJtiAndExpiry(token);
        if (string.IsNullOrEmpty(jti)) return false;

        // Hot path — sólo memoria
        if (_blacklist.ContainsKey(jti)) return true;

        // Cold path — verificación adicional contra BD (defensa en profundidad)
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var revokedInDb = await uow.RevokedTokens.IsRevokedAsync(jti);

        if (revokedInDb)
        {
            // Repoblamos caché para futuras consultas
            _blacklist[jti] = DateTime.UtcNow.AddHours(24);
        }

        return revokedInDb;
    }

    /// <summary>
    /// Carga la blacklist persistida en memoria al arrancar la app.
    /// Se llama desde Program.cs después de Build().
    /// </summary>
    public async System.Threading.Tasks.Task LoadFromDatabaseAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var all = await uow.RevokedTokens.GetAllAsync();

        var loaded = 0;
        foreach (var rt in all.Where(rt => rt.ExpiresAt > DateTime.UtcNow))
        {
            _blacklist[rt.TokenId] = rt.ExpiresAt;
            loaded++;
        }

        _logger.LogInformation("Loaded {Count} active revoked tokens into blacklist cache", loaded);
    }

    private (string jti, DateTime expiresAt) ExtractJtiAndExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return (string.Empty, DateTime.MinValue);

            var jwt = handler.ReadJwtToken(token);

            // Si el token tiene JTI, lo usamos. Si no, usamos el token completo como id (hash).
            var jti = jwt.Id;
            if (string.IsNullOrEmpty(jti))
            {
                jti = ComputeTokenHash(token);
            }

            return (jti, jwt.ValidTo);
        }
        catch
        {
            return (string.Empty, DateTime.MinValue);
        }
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
