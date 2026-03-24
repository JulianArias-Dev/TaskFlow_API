namespace TaskFlow_API.DTOs;

/// <summary>
/// Clase base para respuestas API estandarizadas
/// </summary>
public class ResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public int? StatusCode { get; set; }

    public ResponseDto() { }

    public ResponseDto(T data, string message = "Operación exitosa")
    {
        Success = true;
        Message = message;
        Data = data;
    }

    public ResponseDto(bool success, string message, T? data = default)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public ResponseDto(bool success, string message, List<string> errors, int statusCode = 400)
    {
        Success = success;
        Message = message;
        Errors = errors;
        StatusCode = statusCode;
    }

    public static ResponseDto<T> SuccessResponse(T data, string message = "Operación exitosa")
        => new(data, message);

    public static ResponseDto<T> ErrorResponse(string message, List<string>? errors = null, int statusCode = 400)
        => new(false, message, errors, statusCode);

    public static ResponseDto<T> NotFoundResponse(string message = "Recurso no encontrado")
        => new(false, message, null, 404);

    public static ResponseDto<T> ValidationErrorResponse(List<string> errors)
        => new(false, "Errores de validación", errors, 400);
}

/// <summary>
/// Respuesta para operaciones sin datos específicos
/// </summary>
public class ResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public int? StatusCode { get; set; }

    public ResponseDto() { }

    public ResponseDto(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public ResponseDto(bool success, string message, List<string> errors, int statusCode)
    {
        Success = success;
        Message = message;
        Errors = errors;
        StatusCode = statusCode;
    }

    public static ResponseDto SuccessResponse(string message = "Operación exitosa")
        => new(true, message);

    public static ResponseDto ErrorResponse(string message, List<string>? errors = null, int statusCode = 400)
        => new(false, message, errors ?? new(), statusCode);

    public static ResponseDto NotFoundResponse(string message = "Recurso no encontrado")
        => new(false, message, new() { "Recurso no encontrado" }, 404);

    public static ResponseDto ValidationErrorResponse(List<string> errors)
        => new(false, "Errores de validación", errors, 400);

	public static ResponseDto FailureResponse(string message)
	{
		return new ResponseDto { Success = false, Message = message };
	}
}

/// <summary>
/// Respuesta paginada genérica
/// </summary>
public class PagedResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResponseDto() { }

    public PagedResponseDto(List<T> items, int pageNumber, int pageSize, int totalCount, string message = "Datos recuperados exitosamente")
    {
        Success = true;
        Message = message;
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static PagedResponseDto<T> SuccessResponse(List<T> items, int pageNumber, int pageSize, int totalCount)
        => new(items, pageNumber, pageSize, totalCount);
}
