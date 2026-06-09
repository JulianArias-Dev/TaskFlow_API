namespace TaskFlow_API.Patterns.Observer;

/// <summary>
/// PATRÓN OBSERVER — Define el contrato de eventos del dominio.
/// </summary>
public enum TaskFlowEvent
{
    TaskCreated,
    TaskMoved,
    TaskAssigned,
    TaskDeleted,
    MemberAdded,
    ProjectStatusChanged
}

public class DomainEvent
{
    public TaskFlowEvent EventType { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>Observer — contrato que implementa cada suscriptor.</summary>
public interface ITaskEventObserver
{
    string ObserverName { get; }
    IReadOnlyCollection<TaskFlowEvent> InterestedIn { get; }
    System.Threading.Tasks.Task OnEventAsync(DomainEvent domainEvent);
}

/// <summary>Subject — gestiona suscriptores y dispara eventos.</summary>
public interface ITaskEventPublisher
{
    void Subscribe(ITaskEventObserver observer);
    void Unsubscribe(ITaskEventObserver observer);
    System.Threading.Tasks.Task PublishAsync(DomainEvent domainEvent);
}

public class TaskEventPublisher : ITaskEventPublisher
{
    private readonly List<ITaskEventObserver> _observers = new();
    private readonly ILogger<TaskEventPublisher> _logger;

    public TaskEventPublisher(ILogger<TaskEventPublisher> logger)
    {
        _logger = logger;
    }

    public void Subscribe(ITaskEventObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
        _logger.LogDebug("[Observer] {Name} suscrito", observer.ObserverName);
    }

    public void Unsubscribe(ITaskEventObserver observer)
        => _observers.Remove(observer);

    public async System.Threading.Tasks.Task PublishAsync(DomainEvent domainEvent)
    {
        _logger.LogInformation("[Observer] Evento {Event} publicado", domainEvent.EventType);
        var interested = _observers
            .Where(o => o.InterestedIn.Contains(domainEvent.EventType));

        foreach (var observer in interested)
        {
            try
            {
                await observer.OnEventAsync(domainEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Observer] {Name} falló procesando {Event}",
                    observer.ObserverName, domainEvent.EventType);
            }
        }
    }
}