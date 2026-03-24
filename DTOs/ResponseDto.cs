namespace TaskFlow_API.DTOs;

/// <summary>
/// Clase base para respuestas API estandarizadas
/// </summary>
public class ResponseDto<T>
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public T? Data { get; set; }
	public List<string> Errors { get; set; } = new(); // Inicializado por defecto
	public int? StatusCode { get; set; }

	public ResponseDto() { }

	// Constructor para Éxito
	public ResponseDto(T? data, string message = "Operación exitosa")
	{
		Success = true;
		Message = message;
		Data = data;
		StatusCode = 200;
	}

	// Constructor para Error
	public ResponseDto(bool success, string message, List<string>? errors = null, int statusCode = 400)
	{
		Success = success;
		Message = message;
		Errors = errors ?? new List<string>();
		StatusCode = statusCode;
	}

	public static ResponseDto<T> SuccessResponse(T data, string message = "Operación exitosa")
		=> new(data, message);

	public static ResponseDto<T> ErrorResponse(string message, List<string>? errors = null, int statusCode = 400)
		=> new(false, message, errors, statusCode);

	public static ResponseDto<T> NotFoundResponse(string message = "Recurso no encontrado")
		=> new(false, message, null, 404);
}

/// <summary>
/// Respuesta para operaciones sin datos específicos
/// </summary>
public class ResponseDto
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public List<string> Errors { get; set; } = new(); // Siempre inicializada
	public int StatusCode { get; set; } = 200;

	// Constructor vacío para Serialización (necesario para JSON)
	public ResponseDto() { }

	// Constructor para Éxito
	public ResponseDto(bool success, string message, int statusCode = 200)
	{
		Success = success;
		Message = message;
		StatusCode = statusCode;
	}

	// Constructor para Errores
	public ResponseDto(bool success, string message, List<string>? errors, int statusCode)
	{
		Success = success;
		Message = message;
		Errors = errors ?? new List<string>();
		StatusCode = statusCode;
	}

	// --- Métodos Estáticos (Fábricas) ---
	public static ResponseDto SuccessResponse(string message = "Operación exitosa")
		=> new(true, message, 200);

	public static ResponseDto FailureResponse(string message, int statusCode = 400)
		=> new(false, message, null, statusCode);

	public static ResponseDto ErrorResponse(string message, List<string>? errors = null, int statusCode = 400)
		=> new(false, message, errors, statusCode);

	public static ResponseDto NotFoundResponse(string message = "Recurso no encontrado")
		=> new(false, message, new List<string> { message }, 404);

	public static ResponseDto ValidationErrorResponse(List<string> errors)
		=> new(false, "Errores de validación", errors, 400);
}

/// <summary>
/// Respuesta paginada genérica
/// </summary>
public class PagedResponseDto<T>
{
	public bool Success { get; set; } = true;
	public string Message { get; set; } = string.Empty;
	public List<T> Items { get; set; } = new();

	// Metadatos de paginación
	public int PageNumber { get; set; }
	public int PageSize { get; set; }
	public int TotalCount { get; set; }

	// Propiedades calculadas con protección contra división por cero
	public int TotalPages => PageSize > 0
		? (int)System.Math.Ceiling((double)TotalCount / PageSize)
		: 0;

	public bool HasPreviousPage => PageNumber > 1;
	public bool HasNextPage => PageNumber < TotalPages;

	// Constructor vacío para serialización JSON
	public PagedResponseDto() { }

	public PagedResponseDto(List<T> items, int pageNumber, int pageSize, int totalCount, string message = "Datos recuperados exitosamente")
	{
		Success = true;
		Message = message;
		Items = items ?? new List<T>();
		PageNumber = pageNumber;
		PageSize = pageSize;
		TotalCount = totalCount;
	}

	// Método estático de fábrica para un uso más fluido
	public static PagedResponseDto<T> SuccessResponse(List<T> items, int pageNumber, int pageSize, int totalCount)
		=> new(items, pageNumber, pageSize, totalCount);
}
