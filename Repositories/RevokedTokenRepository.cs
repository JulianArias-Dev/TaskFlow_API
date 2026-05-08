using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

public interface IRevokedTokenRepository : IRepository<RevokedToken>
{
    System.Threading.Tasks.Task<bool> IsRevokedAsync(string tokenId);
    System.Threading.Tasks.Task<int> PurgeExpiredAsync();
}

public class RevokedTokenRepository : Repository<RevokedToken>, IRevokedTokenRepository
{
    public RevokedTokenRepository(TaskFlowDbContext context) : base(context) { }

    public async System.Threading.Tasks.Task<bool> IsRevokedAsync(string tokenId)
        => await _dbSet.AnyAsync(rt => rt.TokenId == tokenId);

    /// <summary>Elimina los tokens cuya fecha de expiración ya pasó (mantenimiento).</summary>
    public async System.Threading.Tasks.Task<int> PurgeExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var expired = _dbSet.Where(rt => rt.ExpiresAt < now);
        _dbSet.RemoveRange(expired);
        return await _context.SaveChangesAsync();
    }
}
