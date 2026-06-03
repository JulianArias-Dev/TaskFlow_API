using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Factory;

/// <summary>
/// Factory Method — define la operación de creación de Tasks pero deja a las
/// subclases concretas decidir el tipo/configuración específica. Cada factory
/// concreta corresponde a un TaskType del catálogo y aplica los defaults
/// razonables (prioridad, type-id) para ese tipo.
///
/// Importante: la entidad creada DEBE incluir ColumnId (FK obligatoria en BD).
/// </summary>
public interface ITaskFactory
{
    TaskEntity CreateTask(string title, string description, Guid columnId);
}

/// <summary>Factoría para tareas tipo BUG (prioridad ALTA por defecto).</summary>
public class BugTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid columnId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            TypeId = 2,        // BUG
            PriorityId = 3,    // HIGH — los bugs nacen con alta prioridad
            StatusId = 1,      // To Do
            ColumnId = columnId
        };
    }
}

/// <summary>Factoría para tareas tipo FEATURE.</summary>
public class FeatureTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid columnId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            TypeId = 1,        // FEATURE
            PriorityId = 2,    // MEDIUM
            StatusId = 1,
            ColumnId = columnId
        };
    }
}

/// <summary>Factoría para tareas tipo TASK (la opción "neutral" / general).</summary>
public class SimpleTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid columnId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            TypeId = 5,        // TASK
            PriorityId = 2,    // MEDIUM
            StatusId = 1,
            ColumnId = columnId
        };
    }
}

/// <summary>Factoría para tareas tipo IMPROVEMENT.</summary>
public class ImprovementTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid columnId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            TypeId = 3,        // IMPROVEMENT
            PriorityId = 1,    // LOW
            StatusId = 1,
            ColumnId = columnId
        };
    }
}

/// <summary>Factoría para tareas tipo RESEARCH.</summary>
public class ResearchTaskFactory : ITaskFactory
{
    public TaskEntity CreateTask(string title, string description, Guid columnId)
    {
        return new TaskEntity
        {
            Title = title,
            Description = description,
            TypeId = 4,        // RESEARCH
            PriorityId = 2,    // MEDIUM
            StatusId = 1,
            ColumnId = columnId
        };
    }
}

/// <summary>
/// Provider que mapea cada TypeId del catálogo a su factoría concreta.
/// Este es el "Factory Method" propiamente dicho: el cliente sólo conoce
/// la abstracción (ITaskFactory) y este provider decide qué implementación
/// usar según el id que recibe.
/// </summary>
public class TaskFactoryProvider
{
    private readonly Dictionary<int, ITaskFactory> _factories;

    public TaskFactoryProvider()
    {
        // Las claves corresponden a TaskTypes.Id seedeados en TaskFlowDbContext:
        //   1=Funcionalidad, 2=Error, 3=Mejora, 4=Investigación, 5=Tarea
        // (La subtarea no es un tipo: es cualquier tarea con ParentTaskId no nulo.)
        _factories = new Dictionary<int, ITaskFactory>
        {
            { 1, new FeatureTaskFactory() },
            { 2, new BugTaskFactory() },
            { 3, new ImprovementTaskFactory() },
            { 4, new ResearchTaskFactory() },
            { 5, new SimpleTaskFactory() }
        };
    }

    public TaskEntity CreateTask(int taskTypeId, string title, string description, Guid columnId)
    {
        if (columnId == Guid.Empty)
            throw new ArgumentException("ColumnId es obligatorio para crear una tarea");

        if (!_factories.TryGetValue(taskTypeId, out var factory))
            throw new ArgumentException($"Tipo de tarea desconocido: {taskTypeId}");

        return factory.CreateTask(title, description, columnId);
    }
}
