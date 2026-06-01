namespace TaskFlow_API.Patterns.State;

/// <summary>
/// PATRÓN STATE — Contrato del estado de un proyecto.
///
/// Cada implementación encapsula:
///   * Qué operaciones (editar, agregar miembros, crear tareas...) están
///     permitidas mientras el proyecto está en ese estado.
///   * A qué otros estados se permite transicionar.
///
/// Esto evita esparcir `if (project.StatusId == X)` por servicios y proxies:
/// la regla vive en un único lugar y es trivial agregar nuevos estados
/// (p. ej. "Archivado") sin tocar los consumidores.
/// </summary>
public interface IProjectState
{
    int StatusId { get; }
    string Name { get; }

    bool CanEdit();
    bool CanAddMembers();
    bool CanRemoveMembers();
    bool CanDelete();
    bool CanClone();
    bool CanGenerateReports();
    bool CanCreateTasks();
    bool CanModifyTasks();

    IReadOnlyCollection<int> AllowedTransitions { get; }
    bool CanTransitionTo(int targetStatusId);
}

/// <summary>
/// Base segura: por defecto TODO está prohibido. Cada estado concreto
/// sobreescribe lo que permite. Así, al agregar una nueva operación en la
/// interfaz, los estados existentes la rechazan hasta que se decida
/// explícitamente lo contrario — fail-closed.
/// </summary>
public abstract class ProjectStateBase : IProjectState
{
    public abstract int StatusId { get; }
    public abstract string Name { get; }

    public virtual bool CanEdit() => false;
    public virtual bool CanAddMembers() => false;
    public virtual bool CanRemoveMembers() => false;
    public virtual bool CanDelete() => false;
    public virtual bool CanClone() => true;
    public virtual bool CanGenerateReports() => true;
    public virtual bool CanCreateTasks() => false;
    public virtual bool CanModifyTasks() => false;

    public virtual IReadOnlyCollection<int> AllowedTransitions => Array.Empty<int>();

    public bool CanTransitionTo(int targetStatusId) =>
        targetStatusId == StatusId || AllowedTransitions.Contains(targetStatusId);
}
