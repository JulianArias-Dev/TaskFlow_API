using System.Net;
using Microsoft.EntityFrameworkCore;
using TaskFlow_API.DTOs;

namespace TaskFlow_API.Middleware;

/// <summary>
/// Middleware global para manejo de excepciones no controladas.
/// Convierte excepciones en respuestas JSON estándar (ResponseDto) y, cuando
/// el entorno no es Production, propaga el mensaje del InnerException más
/// profundo — clave para depurar fallos de EF Core como FK constraints.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async System.Threading.Tasks.Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private System.Threading.Tasks.Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ResponseDto response;

        switch (exception)
        {
            case KeyNotFoundException keyNotFound:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new ResponseDto(false, keyNotFound.Message, new List<string> { keyNotFound.Message }, 404);
                _logger.LogWarning("Recurso no encontrado: {Message}", keyNotFound.Message);
                break;

            case InvalidOperationException invalidOp:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ResponseDto(false, "Operación inválida", new List<string> { invalidOp.Message }, 400);
                _logger.LogWarning("Operación inválida: {Message}", invalidOp.Message);
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ResponseDto(false, "Argumento inválido", new List<string> { argEx.Message }, 400);
                _logger.LogWarning("Argumento inválido: {Message}", argEx.Message);
                break;

            case UnauthorizedAccessException unauthorized:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new ResponseDto(false, "No autorizado", new List<string> { unauthorized.Message }, 401);
                _logger.LogWarning("Acceso no autorizado: {Message}", unauthorized.Message);
                break;

            case DbUpdateException dbUpdate:
                {
                    var detail = GetDeepestMessage(dbUpdate);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new ResponseDto(
                        false,
                        "Error al persistir cambios en la base de datos",
                        new List<string> { detail },
                        400);
                    _logger.LogError(dbUpdate, "DbUpdateException: {Detail}", detail);
                    break;
                }

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var fallback = _env.IsProduction()
                    ? "Ha ocurrido un error inesperado. Por favor, intente más tarde."
                    : GetDeepestMessage(exception);
                response = new ResponseDto(
                    false,
                    "Error interno del servidor",
                    new List<string> { fallback },
                    500);
                _logger.LogError(exception, "Error no controlado: {Message}", exception.Message);
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }

    /// <summary>
    /// Recorre la cadena de InnerException y devuelve el mensaje más interno —
    /// que es donde EF Core / SqlClient suelen poner la causa real (p. ej.
    /// "FOREIGN KEY constraint conflict with table 'AppRoles'").
    /// </summary>
    private static string GetDeepestMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current.Message;
    }
}

/// <summary>
/// Extensión para registrar el middleware de manejo de excepciones.
/// </summary>
public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
