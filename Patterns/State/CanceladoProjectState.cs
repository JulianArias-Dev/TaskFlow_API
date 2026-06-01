namespace TaskFlow_API.Patterns.State;

/// <summary>Proyecto cancelado — solo lectura, eliminable y archivable.</summary>
public sealed class CanceladoProjectState : ProjectStateBase
{
    public const int Id = 4;
    public override int StatusId => Id;
    public override string Name => "Cancelado";

    public override bool CanDelete() => true;

    public override IReadOnlyCollection<int> AllowedTransitions => new[]
    {
        ArchivadoProjectState.Id,
    };
}
