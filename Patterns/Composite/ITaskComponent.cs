using TaskEntity = TaskFlow_API.Models.Task;

namespace TaskFlow_API.Patterns.Composite;

/// <summary>
/// PATRÓN COMPOSITE — Component.
///
/// Define una interfaz común para tareas hoja (sin subtareas) y tareas
/// compuestas (con subtareas anidadas). El cliente trabaja con esta abstracción
/// SIN preocuparse de si la tarea es simple o compuesta — `GetProgress()` se
/// resuelve recursivamente sumando los pesos de los nodos hijos.
/// </summary>
public interface ITaskComponent
{
    Guid Id { get; }
    string Title { get; }
    int StatusId { get; }

    /// <summary>Peso relativo del nodo dentro del padre (default 1 — todos pesan igual).</summary>
    int Weight { get; }

    /// <summary>Total de horas estimadas, agregando hijos si es compuesto.</summary>
    int GetEstimatedHours();

    /// <summary>
    /// % de progreso unificado en rango [0, 100]:
    ///   - Hoja: 100 si Status == Done, sino 0.
    ///   - Compuesto: media ponderada de subtareas según Weight.
    /// </summary>
    double GetProgress();

    /// <summary>Cantidad total de tareas (incluyéndose) en el subárbol.</summary>
    int CountAll();
}
