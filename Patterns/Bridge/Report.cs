using TaskFlow_API.Repositories;

namespace TaskFlow_API.Patterns.Bridge;

/// <summary>
/// PATRÓN BRIDGE — Abstraction.
///
/// Esta es la jerarquía del "qué": qué datos contiene el reporte y cómo se
/// estructuran. Mantiene una referencia al Implementor (`IReportFormat`) y
/// le delega el "cómo" (PDF/CSV/...). Crear un nuevo tipo de reporte (Velocity,
/// Burndown, Audit) NO requiere tocar las clases de formato — y viceversa.
/// </summary>
public abstract class Report
{
    /// <summary>Lado del puente — el formato concreto inyectado.</summary>
    protected readonly IReportFormat Format;

    protected Report(IReportFormat format) => Format = format;

    /// <summary>Construye la estructura del reporte (responsabilidad de la subclase).</summary>
    protected abstract System.Threading.Tasks.Task<ReportDocument> BuildDocumentAsync();

    /// <summary>
    /// Genera el reporte como bytes en el formato configurado.
    /// Devuelve también ContentType y Extension para que el controller
    /// arme la respuesta HTTP (FileResult).
    /// </summary>
    public async System.Threading.Tasks.Task<(byte[] bytes, string contentType, string extension)> GenerateAsync()
    {
        var document = await BuildDocumentAsync();
        var bytes = Format.Render(document);
        return (bytes, Format.ContentType, Format.Extension);
    }
}

/// <summary>
/// Refined Abstraction — Reporte de proyecto con métricas (RF-08).
/// Construye la estructura del documento; delega la serialización al Bridge.
/// </summary>
public class ProjectSummaryReport : Report
{
    private readonly IUnitOfWork _uow;
    private readonly Guid _projectId;

    public ProjectSummaryReport(IReportFormat format, IUnitOfWork uow, Guid projectId)
        : base(format)
    {
        _uow = uow;
        _projectId = projectId;
    }

    protected override async System.Threading.Tasks.Task<ReportDocument> BuildDocumentAsync()
    {
        var project = await _uow.Projects.GetProjectWithTasksAsync(_projectId)
            ?? throw new KeyNotFoundException($"Project {_projectId} not found");

        var tasks = await _uow.Tasks.GetTasksByProjectAsync(_projectId);
        var taskList = tasks.ToList();

        var doc = new ReportDocument
        {
            Title = $"Project Report — {project.Name}",
            Subtitle = project.Description
        };

        // Sección 1 — Resumen
        var summary = new ReportSection
        {
            Heading = "Summary",
            Headers = new() { "Metric", "Value" },
            Rows = new()
            {
                new() { "Total tasks", taskList.Count.ToString() },
                new() { "Completed (status=3)", taskList.Count(t => t.StatusId == 3).ToString() },
                new() { "In progress (status=2)", taskList.Count(t => t.StatusId == 2).ToString() },
                new() { "Blocked (status=4)", taskList.Count(t => t.StatusId == 4).ToString() },
                new() { "Members", project.Members.Count.ToString() },
                new() { "Boards", project.Boards.Count.ToString() }
            }
        };
        doc.Sections.Add(summary);

        // Sección 2 — Detalle de tareas
        var details = new ReportSection
        {
            Heading = "Tasks",
            Headers = new() { "Title", "Status", "Priority", "DueDate", "Estimated", "Actual" }
        };
        foreach (var t in taskList)
        {
            details.Rows.Add(new()
            {
                t.Title,
                t.StatusId.ToString(),
                t.PriorityId.ToString(),
                t.DueDate?.ToString("yyyy-MM-dd") ?? "-",
                t.EstimatedHours.ToString(),
                t.ActualHours.ToString()
            });
        }
        doc.Sections.Add(details);

        return doc;
    }
}
