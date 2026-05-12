using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;
using TaskFlow_API.Patterns.Builder;
using TaskFlow_API.Patterns.Factory;
using TaskFlow_API.Patterns.Prototype;
using TaskEntity = TaskFlow_API.Models.Task;
using TaskStatus = TaskFlow_API.Models.TaskStatus;
using TaskType = TaskFlow_API.Models.TaskType;
using TaskPriority = TaskFlow_API.Models.TaskPriority;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de Task
/// Define operaciones de negocio para tareas
/// </summary>
public interface ITaskService
{
    System.Threading.Tasks.Task<TaskDto?> GetTaskByIdAsync(Guid id);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetAllTasksAsync();
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(int status);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByAssigneeAsync(Guid userId);
    System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetOverdueTasksAsync();
    System.Threading.Tasks.Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto);
    System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(int taskType, string title, string description, Guid columnId);
    System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto);
    System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id);
    System.Threading.Tasks.Task<TaskDto?> CloneTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(int status);
	System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetSubTasksAsync(Guid parentTaskId);
}

/// <summary>
/// Servicio de Task
/// Encapsula la l�gica de negocio para operaciones con tareas
/// Utiliza patrones creacionales: Factory Method, Builder, Prototype
/// </summary>
public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TaskFactoryProvider _taskFactory;
	private readonly INotificationService _notificationService;

	public TaskService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _taskFactory = new TaskFactoryProvider();
        _notificationService = notificationService;
	}

    public async System.Threading.Tasks.Task<TaskDto?> GetTaskByIdAsync(Guid id)
    {
        var task = await _unitOfWork.Tasks.GetTaskWithAssignmentsAsync(id);
        return task != null ? MapToDto(task) : null;
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetAllTasksAsync()
    {
        var tasks = await _unitOfWork.Tasks.GetAllAsync();
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByProjectAsync(Guid projectId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByProjectAsync(projectId);
        return tasks.Select(MapToDto).ToList();
    }

	public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByStatusAsync(int statusId)
	{
		if (statusId <= 0)
		{
			throw new ArgumentException("El ID de estado proporcionado no es v�lido.");
		}

		var tasks = await _unitOfWork.Tasks.GetTasksByStatusAsync(statusId);

		// 3. Mapeamos a DTO
		return tasks.Select(MapToDto).ToList();
	}

	public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetTasksByAssigneeAsync(Guid userId)
    {
        var tasks = await _unitOfWork.Tasks.GetTasksByAssigneeAsync(userId);
        return tasks.Select(MapToDto).ToList();
    }

    public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetOverdueTasksAsync()
    {
        var tasks = await _unitOfWork.Tasks.GetOverdueTasksAsync();
        return tasks.Select(MapToDto).ToList();
    }

	public async System.Threading.Tasks.Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto)
	{
		var column = await _unitOfWork.Columns.GetByIdAsync(createTaskDto.ColumnId);
		if (column == null) throw new KeyNotFoundException("Column not found");

		var board = await _unitOfWork.Boards.GetByIdAsync(column.BoardId);
		if (board == null) throw new KeyNotFoundException("Board not found");

		int typeId = createTaskDto.TypeId > 0 ? createTaskDto.TypeId : 1;
		int priorityId = createTaskDto.PriorityId > 0 ? createTaskDto.PriorityId : 2;

		var task = new TaskEntity
		{
			Title = createTaskDto.Title,
			Description = createTaskDto.Description ?? "",
			TypeId = typeId,
			PriorityId = priorityId,
			// Una tarea nueva siempre nace en "To Do" (Id=1 en TaskStatuses).
			// El cliente no envía StatusId al crear; los movimientos posteriores
			// entre columnas Kanban actualizan ColumnId, no este campo.
			StatusId = 1,
			ColumnId = createTaskDto.ColumnId,
			DueDate = createTaskDto.DueDate,
			EstimatedHours = createTaskDto.EstimatedHours,
			Tags = createTaskDto.Tags ?? new List<string>(),
			ParentTaskId = createTaskDto.ParentTaskId
		};

		// L�gica de Asignaci�n y Notificaci�n
		if (createTaskDto.AssignedToUserId.HasValue)
		{
			var userId = createTaskDto.AssignedToUserId.Value;

			// VERIFICACI�N: �Es miembro del proyecto?
			var isMember = await _unitOfWork.Projects.IsMemberAsync(board.ProjectId, userId);

			if (!isMember)
			{
				throw new InvalidOperationException("El usuario no es miembro de este proyecto y no puede ser asignado.");
			}

			task.Assignments.Add(new TaskAssignment
			{
				UserId = userId,
				AssignedAt = DateTime.UtcNow
			});

			// Persistir la tarea primero para tener el ID generado
			await _unitOfWork.Tasks.AddAsync(task);
			await _unitOfWork.SaveChangesAsync();

			//  Enviar Notificaci�n (App + Email)
			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user != null)
			{
				string subject = "Nueva tarea asignada";
				string content = $@"
                <h3>Hola {user.Name},</h3>
                <p>Se te ha asignado una nueva tarea en el tablero <strong>{board.Name}</strong>.</p>
                <p><strong>Tarea:</strong> {task.Title}</p>
                <p><strong>Prioridad:</strong> {task.Priority}</p>";

				await _notificationService.NotifyAsync(userId, "ASSIGNED", subject, content);
			}
		}
		else
		{
			await _unitOfWork.Tasks.AddAsync(task);
			await _unitOfWork.SaveChangesAsync();
		}

		return MapToDto(task);
	}

	/// <summary>
	/// Factory Method en acción: delega en TaskFactoryProvider la construcción
	/// de la entidad. La columna se valida primero porque es FK obligatoria.
	/// </summary>
	public async System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(int taskTypeId, string title, string description, Guid columnId)
	{
		// Validar que la columna existe — el error de FK que da EF no es legible.
		var column = await _unitOfWork.Columns.GetByIdAsync(columnId);
		if (column == null)
			throw new KeyNotFoundException($"Column with ID {columnId} not found");

		int finalTypeId = taskTypeId > 0 ? taskTypeId : 5; // 5=Task por defecto

		var task = _taskFactory.CreateTask(finalTypeId, title, description, columnId);

		await _unitOfWork.Tasks.AddAsync(task);
		await _unitOfWork.SaveChangesAsync();

		// Recargamos con navegaciones para que MapToDto entregue nombres legibles
		// en Type/Status/Priority en vez de strings vacíos.
		var hydrated = await _unitOfWork.Tasks.GetTaskWithAssignmentsAsync(task.Id) ?? task;
		return MapToDto(hydrated);
	}

	public async System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto)
    {
		var task = await _unitOfWork.Tasks.GetTaskWithAssignmentsAsync(id);
		if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

		// Identificar el Proyecto para validar membres�a (Subiendo por la jerarqu�a)
		var column = await _unitOfWork.Columns.GetByIdAsync(task.ColumnId);
		var board = await _unitOfWork.Boards.GetByIdAsync(column.BoardId);
		var projectId = board.ProjectId;

		// L�gica de Reasignaci�n con Notificaciones Diferenciadas
		if (updateTaskDto.AssignedUserIds != null)
		{
			var oldUserIds = task.Assignments.Select(a => a.UserId).ToList();
			var newUserIds = updateTaskDto.AssignedUserIds.Where(uid => uid != Guid.Empty).ToList();

			// Identificar grupos
			var usersToNotifyNew = newUserIds.Except(oldUserIds).ToList();
			var usersToNotifyRemoved = oldUserIds.Except(newUserIds).ToList();
			var usersToNotifyUpdated = newUserIds.Intersect(oldUserIds).ToList();

			// Limpiar y Reasignar (Validando membres�a)
			task.Assignments.Clear();
			foreach (var userId in newUserIds)
			{
				if (await _unitOfWork.Projects.IsMemberAsync(projectId, userId))
				{
					task.Assignments.Add(new TaskAssignment { UserId = userId, AssignedAt = DateTime.UtcNow });
				}
			}

			// Disparar Notificaciones 
			await NotifyAssignmentChanges(task, usersToNotifyNew, usersToNotifyRemoved, usersToNotifyUpdated);
		}		

		if (!string.IsNullOrEmpty(updateTaskDto.Title))
			task.Title = updateTaskDto.Title;

		if (!string.IsNullOrEmpty(updateTaskDto.Description))
			task.Description = updateTaskDto.Description;

		if (updateTaskDto.StatusId.HasValue && updateTaskDto.StatusId.Value > 0)
			task.StatusId = updateTaskDto.StatusId.Value;
		
		if (updateTaskDto.PriorityId.HasValue && updateTaskDto.PriorityId.Value > 0)
			task.PriorityId = updateTaskDto.PriorityId.Value;		

		if (updateTaskDto.TypeId.HasValue && updateTaskDto.TypeId.Value > 0)
			task.TypeId = updateTaskDto.TypeId.Value;
		
		if (updateTaskDto.DueDate.HasValue)
            task.DueDate = updateTaskDto.DueDate;

        if (updateTaskDto.EstimatedHours.HasValue)
            task.EstimatedHours = updateTaskDto.EstimatedHours.Value;

        if (updateTaskDto.ActualHours.HasValue)
            task.ActualHours = updateTaskDto.ActualHours.Value;

        if (updateTaskDto.Tags != null)
            task.Tags = updateTaskDto.Tags;

		if (updateTaskDto.ColumnId.HasValue && updateTaskDto.ColumnId.Value != Guid.Empty)
			task.ColumnId = updateTaskDto.ColumnId.Value;
			
		//if (updateTaskDto.newColumn != null && updateTaskDto.newColumn.Value != task.ColumnId)
		//{
		//	// Verificar que la nueva columna pertenezca al mismo Tablero/Proyecto
		//	var newColumnExists = await _unitOfWork.Columns.ExistsAsync(updateTaskDto.newColumn.Value);
		//	if (!newColumnExists) throw new KeyNotFoundException("La columna destino no existe.");

		//	task.ColumnId = updateTaskDto.newColumn.Value;

		//	// Aqu� podr�as disparar una notificaci�n espec�fica de "Movimiento de Tarea"
		//}

		task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(task);
    }

	private async System.Threading.Tasks.Task NotifyAssignmentChanges(TaskEntity task, List<Guid> news, List<Guid> removed, List<Guid> updated)
	{
		foreach (var uid in news)
			await _notificationService.NotifyAsync(uid, "ASSIGNED", "Nueva asignación", $"Has sido asignado a la tarea: {task.Title}");

		foreach (var uid in removed)
			await _notificationService.NotifyAsync(uid, "ASSIGNED", "Asignación removida", $"Ya no eres responsable de la tarea: {task.Title}");

		foreach (var uid in updated)
			await _notificationService.NotifyAsync(uid, "STATUS_CHANGE", "Tarea actualizada", $"Se realizaron cambios en la tarea: {task.Title} en la que estás trabajando.");
	}

	public async System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id)
	{
		// Cargamos la tarea incluyendo sus subtareas para verificar
		var task = await _unitOfWork.Tasks.GetByIdWithSubtasksAsync(id);

		if (task == null) return false;

		// Si tiene subtareas, lanzamos una excepci�n de negocio
		if (task.SubTasks != null && task.SubTasks.Any())
		{
			throw new InvalidOperationException(
				$"No se puede eliminar la tarea '{task.Title}' porque tiene {task.SubTasks.Count} subtareas asociadas. Elimina o mueve las subtareas primero.");
		}

		// Si llegamos aqu�, es seguro borrar
		await _unitOfWork.Tasks.DeleteAsync(id);
		await _unitOfWork.SaveChangesAsync();

		return true;
	}

	public async System.Threading.Tasks.Task<TaskDto?> CloneTaskAsync(Guid taskId)
    {
        var originalTask = await _unitOfWork.Tasks.GetByIdAsync(taskId);
        if (originalTask == null)
        {
            return null;
        }

        var clonedTask = (TaskEntity)PrototypeManager.CloneTask(originalTask);
        await _unitOfWork.Tasks.AddAsync(clonedTask);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(clonedTask);
    }

    public async System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId)
    {
        return await _unitOfWork.Tasks.GetTaskCountByProjectAsync(projectId);
    }

	public async System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(int statusId)
	{
		if (statusId <= 0)
		{
			throw new ArgumentException("El ID de estado proporcionado no es v�lido.");
		}

		return await _unitOfWork.Tasks.GetTaskCountByStatusAsync(statusId);
	}

	public async System.Threading.Tasks.Task<IEnumerable<TaskDto>> GetSubTasksAsync(Guid parentTaskId)
	{
		var subTasks = await _unitOfWork.Tasks.GetSubTasksAsync(parentTaskId);
		return subTasks.Select(MapToDto).ToList();
	}

	private TaskDto MapToDto(TaskEntity task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            // Las navegaciones a catálogos pueden venir en null si la query no
            // las incluyó (típico tras un AddAsync recién persistido). Para
            // mostrar el nombre legible usar Name; el id viaja aparte.
            Type = task.Type?.Name ?? string.Empty,
            TypeId = task.TypeId,
            Status = task.Status?.Name ?? string.Empty,
            StatusId = task.StatusId,
            Priority = task.Priority?.Name ?? string.Empty,
            PriorityId = task.PriorityId,
            ColumnId = task.ColumnId,
            ParentTaskId = task.ParentTaskId,
            AssignedUserIds = task.Assignments?.Select(a => a.UserId).ToList() ?? new List<Guid>(),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            EstimatedHours = task.EstimatedHours,
            ActualHours = task.ActualHours,
            Tags = task.Tags ?? new List<string>(),
            SubTaskCount = task.SubTasks?.Count ?? 0,
            CommentCount = task.Comments?.Count ?? 0,
            FileCount = task.Files?.Count ?? 0
        };
    }
}
