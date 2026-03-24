using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskFlow_API.Repositories;

namespace TaskFlow_API.Services;

/// <summary>
/// Interfaz del servicio de autenticación (JWT)
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task<LoginResponseDto?> RegisterAsync(CreateUserDto registerDto);
    Task<bool> VerifyPasswordAsync(string password, string passwordHash);
    string HashPassword(string password);
    string GenerateJwtToken(User user);
}

/// <summary>
/// Servicio de autenticación con JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un usuario y devuelve JWT token
    /// </summary>
    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        // Buscar usuario por email
        var user = await _unitOfWork.Users.GetUserByEmailAsync(loginDto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", loginDto.Email);
            return null;
        }

        // Verificar contraseña
        if (!await VerifyPasswordAsync(loginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login attempt with invalid password for user: {Email}", loginDto.Email);
            return null;
        }

        // Generar token JWT
        var token = GenerateJwtToken(user);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours())
        };
    }

    /// <summary>
    /// Registra un nuevo usuario
    /// </summary>
    public async Task<LoginResponseDto?> RegisterAsync(CreateUserDto registerDto)
    {
        // Verificar si el usuario ya existe
        var existingUser = await _unitOfWork.Users.GetUserByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", registerDto.Email);
            throw new InvalidOperationException($"User with email {registerDto.Email} already exists");
        }

        // Hash de la contraseña
        var passwordHash = HashPassword(registerDto.Password);

        // Crear nuevo usuario
        var user = new User
        {
            Email = registerDto.Email,
            Name = registerDto.Name,
            PasswordHash = passwordHash,
            AvatarUrl = registerDto.AvatarUrl,
            Role = Enum.Parse<UserRole>(registerDto.Role, true),
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {Email} registered successfully", user.Email);

        // Generar token JWT
        var token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours())
        };
    }

    /// <summary>
    /// Verifica si una contraseña coincide con su hash
    /// </summary>
    public async System.Threading.Tasks.Task<bool> VerifyPasswordAsync(string password, string passwordHash)
    {
        return await System.Threading.Tasks.Task.FromResult(BCrypt.Net.BCrypt.Verify(password, passwordHash));
    }

    /// <summary>
    /// Genera un hash bcrypt de la contraseña
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Genera un token JWT para el usuario
    /// </summary>
    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "TaskFlow";
        var audience = jwtSettings["Audience"] ?? "TaskFlowUsers";
        var expirationHours = GetTokenExpirationHours();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Email, user.Email),
            new(System.Security.Claims.ClaimTypes.Name, user.Name),
            new(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenExpirationHours()
    {
        var hours = _configuration.GetSection("JwtSettings")["ExpirationHours"];
        return int.TryParse(hours, out var result) ? result : 24;
    }
}
