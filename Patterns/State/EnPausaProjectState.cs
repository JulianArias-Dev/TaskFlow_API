namespace TaskFlow_API.Patterns.State;

/// <summary>Proyecto en pausa — se puede editar metadata y miembros, pero no tocar tareas.</summary>
public sealed class EnPausaProjectState : ProjectStateBase
{
    public const int Id = 3;
    public override int StatusId => Id;
    public override string Name => "En Pausa";

    public override bool CanEdit() => true;
    public override bool CanAddMembers() => true;
    public override bool CanRemoveMembers() => true;

    public override IReadOnlyCollection<int> AllowedTransitions => new[]
    {
        ActivoProjectState.Id,
        CanceladoProjectState.Id,
    };
}
