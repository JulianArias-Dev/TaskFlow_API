using TaskFlow_API.Patterns.Bridge;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

public interface IReportService
{
    System.Threading.Tasks.Task<(byte[] bytes, string contentType, string extension)>
        GenerateProjectReportAsync(Guid projectId, ReportFormatKind format);
}

public enum ReportFormatKind { Pdf, Csv }

/// <summary>
/// Orquesta el patrón Bridge: combina la abstracción (`ProjectSummaryReport`) con
/// el formato concreto seleccionado por el cliente. Sin acoplar el Service a un
/// formato específico, queda trivial añadir Excel/JSON sólo creando un nuevo `IReportFormat`.
/// </summary>
public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow) => _uow = uow;

    public async System.Threading.Tasks.Task<(byte[] bytes, string contentType, string extension)>
        GenerateProjectReportAsync(Guid projectId, ReportFormatKind format)
    {
        IReportFormat impl = format switch
        {
            ReportFormatKind.Pdf => new PdfReportFormat(),
            ReportFormatKind.Csv => new CsvReportFormat(),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        // Aquí se "tiende" el puente: abstracción ProjectSummaryReport + impl IReportFormat.
        var report = new ProjectSummaryReport(impl, _uow, projectId);
        return await report.GenerateAsync();
    }
}
