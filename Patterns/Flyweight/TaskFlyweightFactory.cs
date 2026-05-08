using System.Collections.Concurrent;

namespace TaskFlow_API.Patterns.Flyweight;

/// <summary>
/// PATRÓN FLYWEIGHT — Flyweight Factory.
///
/// Único punto de creación de los flyweights. Si ya existe uno con la misma
/// key (`{Kind}:{Id}`), se devuelve la instancia cacheada — NO se crea otra.
/// Registrado en DI como Singleton para que todas las requests compartan el cache.
/// </summary>
public class TaskFlyweightFactory : ITaskFlyweightFactory
{
    private readonly ConcurrentDictionary<string, TaskMetadataFlyweight> _cache = new();
    private readonly ILogger<TaskFlyweightFactory> _logger;

    public TaskFlyweightFactory(ILogger<TaskFlyweightFactory> logger)
    {
        _logger = logger;
        SeedDefaults();
    }

    public TaskMetadataFlyweight GetTaskType(int typeId, string name)
        => GetOrAdd("type", typeId, () => BuildType(typeId, name));

    public TaskMetadataFlyweight GetTaskPriority(int priorityId, string name)
        => GetOrAdd("priority", priorityId, () => BuildPriority(priorityId, name));

    public TaskMetadataFlyweight GetTaskStatus(int statusId, string name)
        => GetOrAdd("status", statusId, () => BuildStatus(statusId, name));

    public IReadOnlyDictionary<string, TaskMetadataFlyweight> Snapshot() => _cache;

    public int CacheSize => _cache.Count;

    private TaskMetadataFlyweight GetOrAdd(string kind, int id, Func<TaskMetadataFlyweight> factory)
    {
        var key = $"{kind}:{id}";
        return _cache.GetOrAdd(key, _ =>
        {
            var instance = factory();
            _logger.LogDebug("Flyweight cache MISS — created {Key}", key);
            return instance;
        });
    }

    private static TaskMetadataFlyweight BuildType(int id, string name) => id switch
    {
        1 => new("type", 1, "Feature",     "#3b82f6", "✨", "Nueva funcionalidad"),
        2 => new("type", 2, "Bug",         "#ef4444", "🐛", "Defecto a corregir"),
        3 => new("type", 3, "Improvement", "#10b981", "🚀", "Mejora incremental"),
        4 => new("type", 4, "Research",    "#8b5cf6", "🔬", "Tarea de investigación"),
        5 => new("type", 5, "Task",        "#6b7280", "📋", "Tarea genérica"),
        6 => new("type", 6, "SubTask",     "#9ca3af", "🔗", "Subtarea"),
        _ => new("type", id, name,         "#6b7280", "📄", name),
    };

    private static TaskMetadataFlyweight BuildPriority(int id, string name) => id switch
    {
        1 => new("priority", 1, "LOW",      "#10b981", "▽",  "Baja"),
        2 => new("priority", 2, "MEDIUM",   "#3b82f6", "◆",  "Media"),
        3 => new("priority", 3, "HIGH",     "#f59e0b", "▲",  "Alta"),
        4 => new("priority", 4, "CRITICAL", "#ef4444", "🔥", "Crítica"),
        _ => new("priority", id, name,      "#6b7280", "•",  name),
    };

    private static TaskMetadataFlyweight BuildStatus(int id, string name) => id switch
    {
        1 => new("status", 1, "To Do",       "#6b7280", "○", "Pendiente"),
        2 => new("status", 2, "In Progress", "#3b82f6", "◐", "En progreso"),
        3 => new("status", 3, "Done",        "#10b981", "●", "Finalizada"),
        4 => new("status", 4, "Blocked",     "#ef4444", "⊘", "Bloqueada"),
        _ => new("status", id, name,         "#6b7280", "?", name),
    };

    private void SeedDefaults()
    {
        // Pre-poblamos el cache con los catálogos seed para que la PRIMERA
        // request ya encuentre todo en memoria.
        GetTaskType(1, "Feature"); GetTaskType(2, "Bug"); GetTaskType(3, "Improvement");
        GetTaskType(4, "Research"); GetTaskType(5, "Task"); GetTaskType(6, "SubTask");
        GetTaskPriority(1, "LOW"); GetTaskPriority(2, "MEDIUM");
        GetTaskPriority(3, "HIGH"); GetTaskPriority(4, "CRITICAL");
        GetTaskStatus(1, "To Do"); GetTaskStatus(2, "In Progress");
        GetTaskStatus(3, "Done"); GetTaskStatus(4, "Blocked");
    }
}

public interface ITaskFlyweightFactory
{
    TaskMetadataFlyweight GetTaskType(int typeId, string name);
    TaskMetadataFlyweight GetTaskPriority(int priorityId, string name);
    TaskMetadataFlyweight GetTaskStatus(int statusId, string name);
    IReadOnlyDictionary<string, TaskMetadataFlyweight> Snapshot();
    int CacheSize { get; }
}
