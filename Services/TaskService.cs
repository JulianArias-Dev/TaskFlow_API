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
    System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(int taskType, string title, string description, Guid projectId);
    System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto);
    System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id);
    System.Threading.Tasks.Task<TaskDto?> CloneTaskAsync(Guid taskId);
    System.Threading.Tasks.Task<int> GetTaskCountByProjectAsync(Guid projectId);
    System.Threading.Tasks.Task<int> GetTaskCountByStatusAsync(int status);
}

/// <summary>
/// Servicio de Task
/// Encapsula la lógica de negocio para operaciones con tareas
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
			throw new ArgumentException("El ID de estado proporcionado no es válido.");
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
			ColumnId = createTaskDto.ColumnId,
			DueDate = createTaskDto.DueDate,
			EstimatedHours = createTaskDto.EstimatedHours,
			Tags = createTaskDto.Tags ?? new List<string>(),
			ParentTaskId = createTaskDto.ParentTaskId
		};

		// Lógica de Asignación y Notificación
		if (createTaskDto.AssignedToUserId.HasValue)
		{
			var userId = createTaskDto.AssignedToUserId.Value;

			// VERIFICACIÓN: ¿Es miembro del proyecto?
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

			//  Enviar Notificación (App + Email)
			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user != null)
			{
				string subject = "Nueva tarea asignada";
				string content = $@"
                <h3>Hola {user.Name},</h3>
                <p>Se te ha asignado una nueva tarea en el tablero <strong>{board.Name}</strong>.</p>
                <p><strong>Tarea:</strong> {task.Title}</p>
                <p><strong>Prioridad:</strong> {task.Priority}</p>";

				await _notificationService.NotifyAsync(userId, subject, content);
			}
		}
		else
		{
			await _unitOfWork.Tasks.AddAsync(task);
			await _unitOfWork.SaveChangesAsync();
		}

		return MapToDto(task);
	}

	public async System.Threading.Tasks.Task<TaskDto> CreateTaskByTypeAsync(int taskTypeId, string title, string description, Guid projectId)
	{
		int finalTypeId = taskTypeId > 0 ? taskTypeId : 1;

		// 2. La Factory ahora debe recibir el ID (int)
		var task = _taskFactory.CreateTask(finalTypeId, title, description, projectId);

		// 3. Persistencia
		await _unitOfWork.Tasks.AddAsync(task);
		await _unitOfWork.SaveChangesAsync();

		// 4. Mapeo (Asegúrate de que MapToDto use t.Type.Name)
		return MapToDto(task);
	}

	public async System.Threading.Tasks.Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto)
    {
		var task = await _unitOfWork.Tasks.GetTaskWithAssignmentsAsync(id);
		if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {id} not found");
        }

		// Identificar el Proyecto para validar membresía (Subiendo por la jerarquía)
		var column = await _unitOfWork.Columns.GetByIdAsync(task.ColumnId);
		var board = await _unitOfWork.Boards.GetByIdAsync(column.BoardId);
		var projectId = board.ProjectId;

		// Lógica de Reasignación con Notificaciones Diferenciadas
		if (updateTaskDto.AssignedUserIds != null)
		{
			var oldUserIds = task.Assignments.Select(a => a.UserId).ToList();
			var newUserIds = updateTaskDto.AssignedUserIds.Where(uid => uid != Guid.Empty).ToList();

			// Identificar grupos
			var usersToNotifyNew = newUserIds.Except(oldUserIds).ToList();
			var usersToNotifyRemoved = oldUserIds.Except(newUserIds).ToList();
			var usersToNotifyUpdated = newUserIds.Intersect(oldUserIds).ToList();

			// Limpiar y Reasignar (Validando membresía)
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

		//if (updateTaskDto.newColumn != null && updateTaskDto.newColumn.Value != task.ColumnId)
		//{
		//	// Verificar que la nueva columna pertenezca al mismo Tablero/Proyecto
		//	var newColumnExists = await _unitOfWork.Columns.ExistsAsync(updateTaskDto.newColumn.Value);
		//	if (!newColumnExists) throw new KeyNotFoundException("La columna destino no existe.");

		//	task.ColumnId = updateTaskDto.newColumn.Value;

		//	// Aquí podrías disparar una notificación específica de "Movimiento de Tarea"
		//}

		task.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(task);
    }

	private async System.Threading.Tasks.Task NotifyAssignmentChanges(TaskEntity task, List<Guid> news, List<Guid> removed, List<Guid> updated)
	{
		foreach (var uid in news)
			await _notificationService.NotifyAsync(uid, "Nueva asignación", $"Has sido asignado a la tarea: {task.Title}");

		foreach (var uid in removed)
			await _notificationService.NotifyAsync(uid, "Asignación removida", $"Ya no eres responsable de la tarea: {task.Title}");

		foreach (var uid in updated)
			await _notificationService.NotifyAsync(uid, "Tarea actualizada", $"Se realizaron cambios en la tarea: {task.Title} en la que estás trabajando.");
	}

	public async System.Threading.Tasks.Task<bool> DeleteTaskAsync(Guid id)
	{
		// Cargamos la tarea incluyendo sus subtareas para verificar
		var task = await _unitOfWork.Tasks.GetByIdWithSubtasksAsync(id);

		if (task == null) return false;

		// Si tiene subtareas, lanzamos una excepción de negocio
		if (task.SubTasks != null && task.SubTasks.Any())
		{
			throw new InvalidOperationException(
				$"No se puede eliminar la tarea '{task.Title}' porque tiene {task.SubTasks.Count} subtareas asociadas. Elimina o mueve las subtareas primero.");
		}

		// Si llegamos aquí, es seguro borrar
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
			throw new ArgumentException("El ID de estado proporcionado no es válido.");
		}

		return await _unitOfWork.Tasks.GetTaskCountByStatusAsync(statusId);
	}

	private TaskDto MapToDto(TaskEntity task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type.ToString(),
            Status = task.Status.ToString(),
            Priority = task.Priority.ToString(),
            AssignedUserIds = task.Assignments?.Select(a => a.UserId).ToList() ?? new List<Guid>(),
			CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DueDate = task.DueDate,
            EstimatedHours = task.EstimatedHours,
            ActualHours = task.ActualHours,
            Tags = task.Tags
        };
    }
}
