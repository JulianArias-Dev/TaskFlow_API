using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskFlow_API.Data;
using TaskFlow_API.Mappings;
using TaskFlow_API.Middleware;
using TaskFlow_API.Patterns.Adapter;
using TaskFlow_API.Patterns.Facade;
using TaskFlow_API.Patterns.Flyweight;
using TaskFlow_API.Patterns.Proxy;
using TaskFlow_API.Repositories;
using TaskFlow_API.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------
// Servicios ASP.NET base
// ----------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskFlow API",
        Version = "v1.0",
        Description = "API REST para gestión de tareas Kanban con .NET 9 + patrones de diseño"
    });

    // Soporte para Bearer JWT en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Ingrese: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();

// ----------------------------------------------------------------------
// Entity Framework Core
// ----------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(3)),
    ServiceLifetime.Scoped);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskFlowDbContext>("Database");

builder.Services.AddAutoMapper(typeof(MappingProfile));

// ----------------------------------------------------------------------
// Repositorios + Unit of Work
// ----------------------------------------------------------------------
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITaskLabelRepository, TaskLabelRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();
builder.Services.AddScoped<ISavedFilterRepository, SavedFilterRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ----------------------------------------------------------------------
// Servicios de dominio
// ----------------------------------------------------------------------
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBaseCatalogService, BaseCatalogService>();
builder.Services.AddScoped<ISavedFilterService, SavedFilterService>();
builder.Services.AddScoped<IReportService, ReportService>();

// JWT Blacklist — Singleton para que el cache en memoria se comparta globalmente
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// ----------------------------------------------------------------------
// Patrones estructurales
// ----------------------------------------------------------------------
// ADAPTER — registramos los adapters concretos. NotificationService los recibe
// como IEnumerable<INotificationAdapter> e itera por todos los aplicables.
builder.Services.AddScoped<INotificationAdapter, InAppNotificationAdapter>();
builder.Services.AddScoped<INotificationAdapter, SmtpEmailNotificationAdapter>();
builder.Services.AddScoped<INotificationAdapter, SendGridNotificationAdapter>();

// FACADE — orquesta servicios + Composite para el dashboard.
builder.Services.AddScoped<IProjectFacade, ProjectFacade>();

// FLYWEIGHT — Singleton para que el cache de metadata sea global a la app.
builder.Services.AddSingleton<ITaskFlyweightFactory, TaskFlyweightFactory>();

// PROXY — sustituye a TaskService como implementación de ITaskService.
//   1) Registramos la implementación real con su tipo concreto.
//   2) Registramos ITaskService apuntando al Proxy, que internamente usa el real.
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<ITaskService>(sp =>
{
    var real = sp.GetRequiredService<TaskService>();
    return new TaskServiceProxy(
        real,
        sp.GetRequiredService<IUnitOfWork>(),
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<ILogger<TaskServiceProxy>>());
});

// ----------------------------------------------------------------------
// Email + JWT settings
// ----------------------------------------------------------------------
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["X-Token-Expired"] = "true";
                    context.Response.Headers["Access-Control-Expose-Headers"] = "X-Token-Expired";
                }
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"))
    .AddPolicy("DeveloperOrHigher", policy => policy.RequireRole("Admin", "Manager", "Developer"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH");
    });
});

var app = builder.Build();

// ----------------------------------------------------------------------
// Pipeline HTTP
// ----------------------------------------------------------------------
app.UseGlobalExceptionHandler();
app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseJwtBlacklist();   // Verifica blacklist DESPUÉS de autenticar
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1.0");
        c.RoutePrefix = "swagger";
    });
}

app.MapHealthChecks("/health");

app.MapGet("/", () => new
{
    service = "TaskFlow API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow
});

app.MapControllers();

// Calienta el cache de la blacklist con los tokens persistidos antes de aceptar tráfico.
using (var scope = app.Services.CreateScope())
{
    var blacklist = scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>();
    await blacklist.LoadFromDatabaseAsync();
}

app.Run();
