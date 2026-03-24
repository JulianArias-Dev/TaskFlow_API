using System.Text.Json;
using TaskFlow_API.Models;
using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Prototype;

/// <summary>
/// Interfaz para el patrón Prototype
/// </summary>
public interface IPrototype
{
    object Clone();
}

/// <summary>
/// Implementación avanzada del patrón Prototype para Task
/// Proporciona clonado profundo (Deep Copy) simulando RF-02.5
/// </summary>
public class TaskPrototype : TaskEntity, IPrototype
{
    /// <summary>
    /// Realiza un clonado profundo mediante serialización JSON
    /// Garantiza que todos los tipos de datos se clonan completamente,
    /// incluyendo colecciones y objetos anidados
    /// </summary>
    public new object Clone()
    {
        try
        {
            var json = JsonSerializer.Serialize(this);
            var cloned = JsonSerializer.Deserialize<TaskPrototype>(json);
            
            if (cloned != null)
            {
                cloned.Id = Guid.NewGuid();
                cloned.CreatedAt = DateTime.UtcNow;
                cloned.UpdatedAt = DateTime.UtcNow;
            }

            return cloned ?? throw new InvalidOperationException("Clone deserialization failed");
        }
        catch
        {
            // Fallback a clonado superficial en caso de error
            return new TaskPrototype
            {
                Id = Guid.NewGuid(),
                Title = this.Title,
                Description = this.Description,
                Type = this.Type,
                Status = this.Status,
                Priority = this.Priority,
                ProjectId = this.ProjectId,
                ColumnId = this.ColumnId,
				Assignments = this.Assignments?.Select(a => new TaskAssignment
				{
					UserId = a.UserId,
					TaskId = Guid.Empty, // Se asignará al guardar la nueva tarea
					AssignedAt = DateTime.UtcNow
				}).ToList() ?? new List<TaskAssignment>(),
				CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DueDate = this.DueDate,
                EstimatedHours = this.EstimatedHours,
                ActualHours = 0,
                Tags = new List<string>(this.Tags)
            };
        }
    }
}

/// <summary>
/// Implementación avanzada del patrón Prototype para Project
/// Proporciona clonado profundo (Deep Copy) simulando RF-04.8
/// </summary>
public class ProjectPrototype : Project, IPrototype
{
    /// <summary>
    /// Realiza un clonado profundo de todo el proyecto incluyendo sus tareas
    /// </summary>
    public new object Clone()
    {
        try
        {
            var json = JsonSerializer.Serialize(this);
            var cloned = JsonSerializer.Deserialize<ProjectPrototype>(json);
            
            if (cloned != null)
            {
                cloned.Id = Guid.NewGuid();
                cloned.CreatedAt = DateTime.UtcNow;
                cloned.UpdatedAt = DateTime.UtcNow;
                
                // Clonar cada tarea con nuevo ID
                cloned.Tasks = this.Tasks.Select(t =>
                {
                    var taskClone = (TaskEntity)t.Clone();
                    taskClone.ProjectId = cloned.Id;
                    return taskClone;
                }).ToList();
            }

            return cloned ?? throw new InvalidOperationException("Clone deserialization failed");
        }
        catch
        {
            // Fallback a clonado superficial en caso de error
            return new ProjectPrototype
            {
                Id = Guid.NewGuid(),
                Name = this.Name,
                Description = this.Description,
                Color = this.Color,
                Status = this.Status,
                OwnerId = this.OwnerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                Tasks = this.Tasks.Select(t => (TaskEntity)t.Clone()).ToList(),
                Boards = new(),
                Tags = new(),
                Members = new()
            };
        }
    }
}

/// <summary>
/// Gestor centralizado para operaciones de clonado
/// </summary>
public class PrototypeManager
{
    /// <summary>
    /// Clona una tarea manteniendo su estructura y generando nuevos IDs
    /// </summary>
    public static TaskEntity CloneTask(TaskEntity originalTask)
    {
        return (TaskEntity)originalTask.Clone();
    }

    /// <summary>
    /// Clona un proyecto con todas sus tareas
    /// </summary>
    public static Project CloneProject(Project originalProject)
    {
        return (Project)originalProject.Clone();
    }

    /// <summary>
    /// Clona múltiples tareas
    /// </summary>
    public static List<TaskEntity> CloneTasks(List<TaskEntity> tasks)
    {
        return tasks.Select(t => (TaskEntity)t.Clone()).ToList();
    }
}
