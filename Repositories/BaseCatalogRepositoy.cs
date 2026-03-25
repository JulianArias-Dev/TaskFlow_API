using Microsoft.EntityFrameworkCore;
using System;
using TaskFlow_API.Data;
using TaskFlow_API.Models;

namespace TaskFlow_API.Repositories
{
	public interface ICatalogRepository
	{
		Task<IEnumerable<TaskType>> GetTaskTypesAsync();
		Task<IEnumerable<TaskPriority>> GetTaskPrioritiesAsync();
		Task<IEnumerable<TaskFlow_API.Models.TaskStatus>> GetTaskStatusesAsync();
		Task<IEnumerable<AppRole>> GetAppRolesAsync();
		Task<IEnumerable<ProjectRole>> GetProjectRolesAsync();
		Task<IEnumerable<ProjectStatus>> GetProjectStatusesAsync();
	}

	public class CatalogRepository : ICatalogRepository
	{
		private readonly TaskFlowDbContext _context;

		public CatalogRepository(TaskFlowDbContext context) => _context = context;

		public async Task<IEnumerable<TaskType>> GetTaskTypesAsync() => await _context.TaskTypes.AsNoTracking().ToListAsync();
		public async Task<IEnumerable<TaskPriority>> GetTaskPrioritiesAsync() => await _context.TaskPriorities.AsNoTracking().ToListAsync();
		public async Task<IEnumerable<TaskFlow_API.Models.TaskStatus>> GetTaskStatusesAsync() => await _context.TaskStatuses.AsNoTracking().ToListAsync();
		public async Task<IEnumerable<AppRole>> GetAppRolesAsync() => await _context.AppRoles.AsNoTracking().ToListAsync();
		public async Task<IEnumerable<ProjectRole>> GetProjectRolesAsync() => await _context.ProjectRoles.AsNoTracking().ToListAsync();
		public async Task<IEnumerable<ProjectStatus>> GetProjectStatusesAsync() => await _context.ProjectStatuses.AsNoTracking().ToListAsync();
	}
}
