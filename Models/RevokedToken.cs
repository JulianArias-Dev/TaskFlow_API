namespace TaskFlow_API.Models;

/// <summary>
/// Token JWT revocado (blacklist). Permite invalidar un token antes de su
/// expiración natural cuando el usuario hace logout.
/// </summary>
public class RevokedToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>JTI o hash del token revocado. Indexado para lookup O(1) en BD.</summary>
    public string TokenId { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    /// <summary>Fecha en la que expira naturalmente el token (para purgar registros viejos).</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }
}
