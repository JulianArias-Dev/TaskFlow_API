namespace TaskFlow_API.Patterns.State;

/// <summary>Proyecto archivado — estado terminal de solo lectura. Para reabrirlo hay que clonar.</summary>
public sealed class ArchivadoProjectState : ProjectStateBase
{
    public const int Id = 5;
    public override int StatusId => Id;
    public override string Name => "Archivado";

    // AllowedTransitions: vacío (heredado de ProjectStateBase) — estado terminal.
}
