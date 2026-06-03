namespace TaskFlow_API.Patterns.State;

/// <summary>Proyecto activo — operaciones completas y único estado que puede modificar tareas.</summary>
public sealed class ActivoProjectState : ProjectStateBase
{
    public const int Id = 1;
    public override int StatusId => Id;
    public override string Name => "Activo";

    public override bool CanEdit() => true;
    public override bool CanAddMembers() => true;
    public override bool CanRemoveMembers() => true;
    public override bool CanDelete() => true;
    public override bool CanCreateTasks() => true;
    public override bool CanModifyTasks() => true;

    public override IReadOnlyCollection<int> AllowedTransitions => new[]
    {
        CompletadoProjectState.Id,
        EnPausaProjectState.Id,
        CanceladoProjectState.Id,
        ArchivadoProjectState.Id, // Archivar siempre disponible — equivalente a "guardar para histórico".
    };
}
