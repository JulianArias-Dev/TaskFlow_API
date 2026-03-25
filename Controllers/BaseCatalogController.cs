using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BaseCatalogController : ControllerBase
	{
		private readonly IBaseCatalogService _catalogService;

		public BaseCatalogController(IBaseCatalogService catalogService)
		{
			_catalogService = catalogService;
		}

		[HttpGet("task-types")]
		public async Task<ActionResult<IEnumerable<CatalogDto>>> GetTaskTypes()
			=> Ok(await _catalogService.GetTaskTypesAsync());

		[HttpGet("task-priorities")]
		public async Task<ActionResult<IEnumerable<CatalogDto>>> GetTaskPriorities()
			=> Ok(await _catalogService.GetTaskPrioritiesAsync());

		[HttpGet("task-status")]
		public async Task<ActionResult<IEnumerable<CatalogDto>>> GetTaskStatuses()
			=> Ok(await _catalogService.GetTaskStatusesAsync());

		[HttpGet("app-roles")]
		public async Task<ActionResult<IEnumerable<CatalogDto>>> GetAppRoles()
			=> Ok(await _catalogService.GetAppRolesAsync());

		[HttpGet("project-roles")]
		public async Task<ActionResult<IEnumerable<CatalogDto>>> GetProjectRoles()
			=> Ok(await _catalogService.GetProjectRolesAsync());

		[HttpGet("project-status")]
		public async Task<ActionResult<IEnumerable<CatalogDto>>> GetProjectStatuses()
			=> Ok(await _catalogService.GetProjectStatusesAsync());

	}
}
