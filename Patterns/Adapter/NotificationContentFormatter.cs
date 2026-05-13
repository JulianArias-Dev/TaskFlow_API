namespace TaskFlow_API.Patterns.Adapter;

/// <summary>
/// Helper para formatear notificaciones en 3 partes: Saludo, Mensaje, Cierre.
/// Los servicios construyen el contenido con este formato; los adapters de email
/// lo transforman en HTML con estilos consistentes.
/// </summary>
public static class NotificationContentFormatter
{
    private const string Separator = "  ";

    /// <summary>
    /// Construye una notificación estructurada en 3 partes.
    /// </summary>
    public static string Build(string greeting, string message, string closing = "Saludos,\nEl equipo de TaskFlow")
        => $"{greeting}{Separator}{message}{Separator}{closing}";

    /// <summary>
    /// Parsea el contenido estructurado en sus 3 partes.
    /// Si no contiene separadores, devuelve el contenido como mensaje y partes vacías.
    /// </summary>
    public static (string greeting, string message, string closing) Parse(string content)
    {
        var parts = content.Split(new[] { Separator }, StringSplitOptions.None);

        return parts.Length == 3
            ? (parts[0].Trim(), parts[1].Trim(), parts[2].Trim())
            : (string.Empty, content.Trim(), string.Empty);
    }

    /// <summary>
    /// Transforma contenido estructurado en HTML con estilos visuales.
    /// </summary>
    public static string FormatAsHtml(string content)
    {
        var (greeting, message, closing) = Parse(content);

        var greetingHtml = string.IsNullOrEmpty(greeting) ? string.Empty : $"<p style=\"font-size: 16px; margin-bottom: 15px;\"><strong>{HtmlEncode(greeting)}</strong></p>";
        var messageHtml = string.IsNullOrEmpty(message) ? string.Empty : $"<p style=\"margin-bottom: 15px; line-height: 1.6;\">{HtmlEncode(message).Replace("\n", "<br>")}</p>";
        var closingHtml = string.IsNullOrEmpty(closing) ? string.Empty : $"<p style=\"margin-top: 20px; color: #7f8c8d;\">{HtmlEncode(closing).Replace("\n", "<br>")}</p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .footer {{ border-top: 1px solid #ecf0f1; margin-top: 30px; padding-top: 15px; font-size: 12px; color: #95a5a6; }}
    </style>
</head>
<body>
    <div class='container'>
        {greetingHtml}
        {messageHtml}
        {closingHtml}
        <div class='footer'>
            <p>Este es un mensaje automático de TaskFlow. Por favor no responder a este correo.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string HtmlEncode(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
