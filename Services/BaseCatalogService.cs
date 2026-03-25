using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services
{
	public interface IBaseCatalogService
	{
		Task<IEnumerable<CatalogDto>> GetTaskTypesAsync();
		Task<IEnumerable<CatalogDto>> GetTaskPrioritiesAsync();
		Task<IEnumerable<CatalogDto>> GetTaskStatusesAsync();
		Task<IEnumerable<CatalogDto>> GetAppRolesAsync();
		Task<IEnumerable<CatalogDto>> GetProjectRolesAsync();
		Task<IEnumerable<CatalogDto>> GetProjectStatusesAsync();
	}

	public class BaseCatalogService : IBaseCatalogService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public BaseCatalogService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		// Usamos el repositorio genérico a través del UnitOfWork o inyectado directamente
		public async Task<IEnumerable<CatalogDto>> GetTaskTypesAsync()
			=> _mapper.Map<IEnumerable<CatalogDto>>(await _unitOfWork.Catalog.GetTaskTypesAsync());

		public async Task<IEnumerable<CatalogDto>> GetTaskPrioritiesAsync()
			=> _mapper.Map<IEnumerable<CatalogDto>>(await _unitOfWork.Catalog.GetTaskPrioritiesAsync());

		public async Task<IEnumerable<CatalogDto>> GetTaskStatusesAsync()
			=> _mapper.Map<IEnumerable<CatalogDto>>(await _unitOfWork.Catalog.GetTaskStatusesAsync());

		public async Task<IEnumerable<CatalogDto>> GetAppRolesAsync()
			=> _mapper.Map<IEnumerable<CatalogDto>>(await _unitOfWork.Catalog.GetAppRolesAsync());

		public async Task<IEnumerable<CatalogDto>> GetProjectRolesAsync()
			=> _mapper.Map<IEnumerable<CatalogDto>>(await _unitOfWork.Catalog.GetProjectRolesAsync());

		public async Task<IEnumerable<CatalogDto>> GetProjectStatusesAsync()
			=> _mapper.Map<IEnumerable<CatalogDto>>(await _unitOfWork.Catalog.GetProjectStatusesAsync());
	}
}
