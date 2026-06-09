using TaskFlow_API.Patterns.Observer;

namespace TaskFlow_API.Patterns.Observer.ConcreteObservers;

/// <summary>Observer concreto — log de auditoría para cualquier evento.</summary>
public class AuditObserver : ITaskEventObserver
{
    private readonly ILogger<AuditObserver> _logger;

    public AuditObserver(ILogger<AuditObserver> logger)
    {
        _logger = logger;
    }

    public string ObserverName => "AuditObserver";

    public IReadOnlyCollection<TaskFlowEvent> InterestedIn =>
        Enum.GetValues<TaskFlowEvent>();

    public System.Threading.Tasks.Task OnEventAsync(DomainEvent domainEvent)
    {
        _logger.LogInformation(
            "[Audit] {Event} — Project={ProjectId} Task={TaskId} User={UserId} — {Description}",
            domainEvent.EventType, domainEvent.ProjectId,
            domainEvent.TaskId, domainEvent.UserId,
            domainEvent.Description);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
