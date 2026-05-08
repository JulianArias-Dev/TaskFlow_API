using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories;

public interface ISavedFilterRepository : IRepository<SavedFilter>
{
    System.Threading.Tasks.Task<IEnumerable<SavedFilter>> GetByUserAsync(Guid userId);
}

public class SavedFilterRepository : Repository<SavedFilter>, ISavedFilterRepository
{
    public SavedFilterRepository(TaskFlowDbContext context) : base(context) { }

    public async System.Threading.Tasks.Task<IEnumerable<SavedFilter>> GetByUserAsync(Guid userId)
        => await _dbSet.Where(sf => sf.UserId == userId)
                       .OrderByDescending(sf => sf.UpdatedAt)
                       .ToListAsync();
}
