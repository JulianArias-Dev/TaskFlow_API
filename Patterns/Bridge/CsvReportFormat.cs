using System.Globalization;
using System.Text;

namespace TaskFlow_API.Patterns.Bridge;

/// <summary>
/// Concrete Implementor — renderiza el documento como CSV (UTF-8 con BOM, separador `,`).
/// </summary>
public class CsvReportFormat : IReportFormat
{
    public string ContentType => "text/csv";
    public string Extension => "csv";

    public byte[] Render(ReportDocument document)
    {
        var sb = new StringBuilder();

        // Encabezado del documento como comentarios
        sb.AppendLine($"# {document.Title}");
        if (!string.IsNullOrEmpty(document.Subtitle))
            sb.AppendLine($"# {document.Subtitle}");
        sb.AppendLine($"# Generated: {document.GeneratedAt.ToString("o", CultureInfo.InvariantCulture)}");
        sb.AppendLine();

        foreach (var section in document.Sections)
        {
            sb.AppendLine($"## {section.Heading}");
            sb.AppendLine(string.Join(",", section.Headers.Select(EscapeCsv)));
            foreach (var row in section.Rows)
                sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
            sb.AppendLine();
        }

        // BOM UTF-8 para que Excel lo abra correctamente
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        return bom.Concat(body).ToArray();
    }

    private static string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        var needsQuoting = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');
        if (!needsQuoting) return field;
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
