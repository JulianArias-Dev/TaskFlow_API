namespace TaskFlow_API.Patterns.Singleton;

/// <summary>
/// Implementación del patrón Singleton para garantizar una única instancia
/// del contexto de base de datos en toda la aplicación.
/// Nota: En ASP.NET Core con Dependency Injection, el contexto se registra
/// con ServiceLifetime.Singleton en Program.cs, lo que proporciona esta garantía.
/// Esta clase documenta el patrón implementado.
/// </summary>
public static class TaskFlowDbContextSingleton
{
    private static volatile TaskFlow_API.Data.TaskFlowDbContext? _instance;
    private static readonly object _syncRoot = new object();

    public static TaskFlow_API.Data.TaskFlowDbContext GetInstance(TaskFlow_API.Data.TaskFlowDbContext context)
    {
        if (_instance == null)
        {
            lock (_syncRoot)
            {
                if (_instance == null)
                {
                    _instance = context;
                }
            }
        }
        return _instance;
    }
}
