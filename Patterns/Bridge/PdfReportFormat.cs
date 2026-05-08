using System.Text;

namespace TaskFlow_API.Patterns.Bridge;

/// <summary>
/// Concrete Implementor — renderiza el documento como PDF.
///
/// Para mantener el proyecto sin dependencias pesadas (QuestPDF, iTextSharp...),
/// generamos un PDF mínimo válido a mano. En producción se reemplazaría por
/// QuestPDF o similar SIN tocar la jerarquía de Reports — esa es la magia del Bridge.
/// </summary>
public class PdfReportFormat : IReportFormat
{
    public string ContentType => "application/pdf";
    public string Extension => "pdf";

    public byte[] Render(ReportDocument document)
    {
        // PDF 1.4 mínimo válido — un único stream con texto.
        var content = BuildContentStream(document);
        var contentBytes = Encoding.ASCII.GetBytes(content);

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");

        // Tracking de offsets para la xref table
        var offsets = new List<long>();
        long currentOffset = sb.Length;

        // Object 1 — Catalog
        offsets.Add(currentOffset);
        var obj1 = "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n";
        sb.Append(obj1); currentOffset += obj1.Length;

        // Object 2 — Pages
        offsets.Add(currentOffset);
        var obj2 = "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n";
        sb.Append(obj2); currentOffset += obj2.Length;

        // Object 3 — Page
        offsets.Add(currentOffset);
        var obj3 = "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                   "/Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n";
        sb.Append(obj3); currentOffset += obj3.Length;

        // Object 4 — Content stream
        offsets.Add(currentOffset);
        var obj4 = $"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream\nendobj\n";
        sb.Append(obj4); currentOffset += obj4.Length;

        // Object 5 — Font
        offsets.Add(currentOffset);
        var obj5 = "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n";
        sb.Append(obj5); currentOffset += obj5.Length;

        // Cross-reference table
        var xrefStart = currentOffset;
        sb.Append("xref\n0 6\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var off in offsets)
            sb.Append($"{off:D10} 00000 n \n");

        // Trailer
        sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n");
        sb.Append($"startxref\n{xrefStart}\n%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string BuildContentStream(ReportDocument doc)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n/F1 18 Tf\n50 750 Td\n");
        sb.Append($"({EscapePdfString(doc.Title)}) Tj\n");
        sb.Append("/F1 11 Tf\n0 -20 Td\n");
        sb.Append($"({EscapePdfString(doc.Subtitle)}) Tj\n");
        sb.Append("/F1 9 Tf\n0 -15 Td\n");
        sb.Append($"(Generated: {doc.GeneratedAt:yyyy-MM-dd HH:mm} UTC) Tj\n");

        foreach (var section in doc.Sections)
        {
            sb.Append("/F1 13 Tf\n0 -25 Td\n");
            sb.Append($"({EscapePdfString(section.Heading)}) Tj\n");
            sb.Append("/F1 9 Tf\n");

            // Headers
            sb.Append("0 -15 Td\n");
            sb.Append($"({EscapePdfString(string.Join(" | ", section.Headers))}) Tj\n");

            // Rows (limitado para no desbordar la página)
            foreach (var row in section.Rows.Take(30))
            {
                sb.Append("0 -12 Td\n");
                sb.Append($"({EscapePdfString(string.Join(" | ", row))}) Tj\n");
            }
        }

        sb.Append("ET");
        return sb.ToString();
    }

    private static string EscapePdfString(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
