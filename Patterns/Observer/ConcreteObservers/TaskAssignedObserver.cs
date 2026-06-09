using TaskFlow_API.Services;
using TaskFlow_API.Patterns.Observer;

namespace TaskFlow_API.Patterns.Observer.ConcreteObservers;

/// <summary>Observer concreto — envía notificación in-app cuando se asigna una tarea.</summary>
public class TaskAssignedObserver : ITaskEventObserver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TaskAssignedObserver(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public string ObserverName => "TaskAssignedObserver";
    public IReadOnlyCollection<TaskFlowEvent> InterestedIn =>
        new[] { TaskFlowEvent.TaskAssigned };

    public async System.Threading.Tasks.Task OnEventAsync(DomainEvent domainEvent)
    {
        if (!domainEvent.UserId.HasValue) return;
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notificationService.NotifyAsync(
            domainEvent.UserId.Value,
            "ASSIGNED",
            "Nueva tarea asignada",
            domainEvent.Description);
    }
}
