using System.Net;
using System.Text.Json;
using TaskFlow_API.DTOs;

namespace TaskFlow_API.Middleware;

/// <summary>
/// Middleware global para manejo de excepciones no controladas
/// Convierte excepciones en respuestas JSON estándar
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

    private static System.Threading.Tasks.Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ResponseDto();
        var logger = context.RequestServices.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();

        switch (exception)
        {
            case KeyNotFoundException keyNotFound:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new ResponseDto(false, keyNotFound.Message, new List<string> { keyNotFound.Message }, 404);
                logger.LogWarning("Recurso no encontrado: {Message}", keyNotFound.Message);
                break;

            case InvalidOperationException invalidOp:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ResponseDto(false, "Operación inválida", new List<string> { invalidOp.Message }, 400);
                logger.LogWarning("Operación inválida: {Message}", invalidOp.Message);
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ResponseDto(false, "Argumento inválido", new List<string> { argEx.Message }, 400);
                logger.LogWarning("Argumento inválido: {Message}", argEx.Message);
                break;

            case UnauthorizedAccessException unauthorized:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new ResponseDto(false, "No autorizado", new List<string> { unauthorized.Message }, 401);
                logger.LogWarning("Acceso no autorizado: {Message}", unauthorized.Message);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ResponseDto(
                    false,
                    "Error interno del servidor",
                    new List<string> { "Ha ocurrido un error inesperado. Por favor, intente más tarde." },
                    500);
                logger.LogError(exception, "Error no controlado: {Message}", exception.Message);
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extensión para registrar el middleware de manejo de excepciones
/// </summary>
public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
