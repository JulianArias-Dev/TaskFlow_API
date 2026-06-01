namespace TaskFlow_API.Patterns.State;

/// <summary>
/// Fábrica estática que resuelve el `IProjectState` concreto a partir de un
/// StatusId (los mismos IDs que viven en la tabla `ProjectStatuses`).
/// Las instancias son stateless — se podría cachear una sola, pero crearlas
/// es trivial y mantiene el código simple.
/// </summary>
public static class ProjectStateFactory
{
    private static readonly Dictionary<int, Func<IProjectState>> _map = new()
    {
        { ActivoProjectState.Id,     () => new ActivoProjectState() },
        { CompletadoProjectState.Id, () => new CompletadoProjectState() },
        { EnPausaProjectState.Id,    () => new EnPausaProjectState() },
        { CanceladoProjectState.Id,  () => new CanceladoProjectState() },
        { ArchivadoProjectState.Id,  () => new ArchivadoProjectState() },
    };

    public static IProjectState For(int statusId) =>
        _map.TryGetValue(statusId, out var factory)
            ? factory()
            : throw new InvalidOperationException(
                $"ProjectStatusId {statusId} no está registrado en ProjectStateFactory.");

    public static IProjectState For(Models.Project project) => For(project.StatusId);
}
