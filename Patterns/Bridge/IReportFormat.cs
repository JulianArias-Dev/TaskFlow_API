namespace TaskFlow_API.Patterns.Bridge;

/// <summary>
/// PATRÓN BRIDGE — Implementor.
///
/// Esta interfaz es el "lado de la implementación". La abstracción `Report` (el
/// QUÉ va en el reporte) se desacopla totalmente del CÓMO se serializa.
/// La jerarquía de Reports (Project/Tasks/Velocity/etc.) y la jerarquía de
/// formatos (PDF/CSV/JSON/Excel) crecen INDEPENDIENTEMENTE.
/// </summary>
public interface IReportFormat
{
    /// <summary>MIME type (application/pdf, text/csv, ...).</summary>
    string ContentType { get; }

    /// <summary>Extensión sugerida ("pdf", "csv", ...).</summary>
    string Extension { get; }

    /// <summary>Renderiza el documento estructurado a bytes.</summary>
    byte[] Render(ReportDocument document);
}

/// <summary>
/// Documento intermedio neutro — la "estructura" que ambos lados del Bridge
/// comparten. Cualquier reporte (la abstracción) lo construye, y cualquier
/// formato (la implementación) lo serializa.
/// </summary>
public class ReportDocument
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<ReportSection> Sections { get; set; } = new();
}

public class ReportSection
{
    public string Heading { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}
