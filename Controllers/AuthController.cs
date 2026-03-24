using Microsoft.AspNetCore.Mvc;
using TaskFlow_API.DTOs;
using TaskFlow_API.Services;

namespace TaskFlow_API.Controllers;

/// <summary>
/// Controller para autenticación y autorización
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Inicia sesión y devuelve JWT token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseDto<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseDto<LoginResponseDto>>> Login(LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                _logger.LogWarning("Login failed for email: {Email}", loginDto.Email);
                return Unauthorized(ResponseDto.ErrorResponse(
                    "Invalid email or password",
                    new List<string> { "Las credenciales proporcionadas son inválidas" }, 401));
            }

            _logger.LogInformation("User {Email} logged in successfully", loginDto.Email);
            return Ok(ResponseDto<LoginResponseDto>.SuccessResponse(result, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error: {Message}", ex.Message);
            return BadRequest(ResponseDto.ErrorResponse("Login failed", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Registra un nuevo usuario
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ResponseDto<LoginResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseDto<LoginResponseDto>>> Register(CreateUserDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ResponseDto.ValidationErrorResponse(errors));
        }

        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            _logger.LogInformation("User {Email} registered successfully", registerDto.Email);
            return CreatedAtAction(nameof(Login), null, 
                ResponseDto<LoginResponseDto>.SuccessResponse(result!, "User registered successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return Conflict(ResponseDto.ErrorResponse(ex.Message, new List<string> { ex.Message }, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error: {Message}", ex.Message);
            return BadRequest(ResponseDto.ErrorResponse("Registration failed", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// Valida el token JWT actual
    /// </summary>
    [HttpGet("validate")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(ResponseDto<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status401Unauthorized)]
    public ActionResult<ResponseDto<object>> ValidateToken()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ResponseDto.ErrorResponse("Invalid or expired token", 
                new List<string> { "Token is invalid or has expired" }, 401));
        }

        var tokenInfo = new
        {
            userId,
            email,
            name,
            role,
            issuedAt = DateTime.UtcNow,
            validatedAt = DateTime.UtcNow
        };

        return Ok(ResponseDto<object>.SuccessResponse(tokenInfo, "Token is valid"));
    }
}
