using TaskFlow_API.Services;
using TaskFlow_API.Patterns.Observer;

namespace TaskFlow_API.Patterns.Observer.ConcreteObservers;

/// <summary>Observer concreto — notifica cuando una tarea es movida a "Done".</summary>
public class TaskCompletedObserver : ITaskEventObserver
{
    private readonly ILogger<TaskCompletedObserver> _logger;

    public TaskCompletedObserver(ILogger<TaskCompletedObserver> logger)
        => _logger = logger;

    public string ObserverName => "TaskCompletedObserver";
    public IReadOnlyCollection<TaskFlowEvent> InterestedIn =>
        new[] { TaskFlowEvent.TaskMoved };

    public System.Threading.Tasks.Task OnEventAsync(DomainEvent domainEvent)
    {
        if (domainEvent.Metadata.TryGetValue("columnName", out var colName)
            && (colName.Contains("done", StringComparison.OrdinalIgnoreCase)
                || colName.Contains("finalizado", StringComparison.OrdinalIgnoreCase)
                || colName.Contains("hecho", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("[Observer] Tarea {TaskId} completada", domainEvent.TaskId);
        }
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
