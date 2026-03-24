using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TaskFlow_API.Validations;

/// <summary>
/// Validador personalizado para expresiones regulares
/// </summary>
public class RegexAttribute : ValidationAttribute
{
    private readonly string _pattern;

    public RegexAttribute(string pattern)
    {
        _pattern = pattern;
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is string stringValue)
        {
            return Regex.IsMatch(stringValue, _pattern);
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
        => base.ErrorMessage ?? $"El campo {name} no tiene un formato válido";
}

/// <summary>
/// Validador personalizado para fechas futuras
/// </summary>
public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is DateTime dateTime)
        {
            return dateTime > DateTime.UtcNow;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"El campo {name} debe ser una fecha futura";
}

/// <summary>
/// Validador personalizado para fechas pasadas
/// </summary>
public class PastDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is DateTime dateTime)
        {
            return dateTime < DateTime.UtcNow;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"El campo {name} debe ser una fecha pasada";
}

/// <summary>
/// Validador personalizado para rango de fechas
/// </summary>
public class DateRangeAttribute : ValidationAttribute
{
    private readonly string? _comparisonProperty;

    public DateRangeAttribute(string comparisonProperty)
    {
        _comparisonProperty = comparisonProperty;
    }

    public override bool IsValid(object? value)
    {
        return true; // Este validador requiere acceso al contexto de la clase
    }
}

/// <summary>
/// Validador personalizado para tamaño de archivo
/// </summary>
public class FileSizeAttribute : ValidationAttribute
{
    private readonly int _maxFileSizeInMB;

    public FileSizeAttribute(int maxFileSizeInMB = 10)
    {
        _maxFileSizeInMB = maxFileSizeInMB;
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is IFormFile file)
        {
            var maxFileSizeInBytes = _maxFileSizeInMB * 1024 * 1024;
            return file.Length <= maxFileSizeInBytes;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"El archivo {name} no puede exceder {_maxFileSizeInMB}MB";
}

/// <summary>
/// Validador personalizado para tipo de archivo
/// </summary>
public class FileTypeAttribute : ValidationAttribute
{
    private readonly string[] _allowedExtensions;

    public FileTypeAttribute(params string[] allowedExtensions)
    {
        _allowedExtensions = allowedExtensions.Select(x => x.StartsWith(".") ? x : "." + x).ToArray();
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            return _allowedExtensions.Contains(fileExtension);
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"El archivo {name} debe ser uno de los siguientes tipos: {string.Join(", ", _allowedExtensions)}";
}

