namespace TaskFlow_API.Patterns.State;

/// <summary>Proyecto completado — solo lectura. Puede reabrirse a Activo o archivarse.</summary>
public sealed class CompletadoProjectState : ProjectStateBase
{
    public const int Id = 2;
    public override int StatusId => Id;
    public override string Name => "Completado";

    public override IReadOnlyCollection<int> AllowedTransitions => new[]
    {
        ActivoProjectState.Id,
        ArchivadoProjectState.Id,
    };
}
