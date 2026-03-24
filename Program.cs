using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TaskFlow_API.Data;
using TaskFlow_API.Mappings;
using TaskFlow_API.Middleware;
using TaskFlow_API.Repositories;
using TaskFlow_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskFlow API",
        Version = "v1.0",
        Description = "API REST para gestión de tareas Kanban con .NET 9"
    });
});

// Entity Framework Core - SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(3)),
    ServiceLifetime.Scoped);

// Health Checks
// En la sección de servicios
builder.Services.AddHealthChecks()
	.AddDbContextCheck<TaskFlowDbContext>("Database"); // .NET ya sabe cómo probar el DbContext solo

//builder.Services.AddHealthChecks()
//    .AddCheck("Database", () =>
//    {
//        try
//        {
//            var dbContext = builder.Services.BuildServiceProvider().GetRequiredService<TaskFlowDbContext>();
//            if (dbContext.Database.CanConnect())
//            {
//                return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
//                    Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, 
//                    "Database connected");
//            }
//            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
//                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
//                "Cannot connect to database");
//        }
//        catch
//        {
//            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
//                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, 
//                "Database error");
//        }
//    });

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register Repositories and Unit of Work
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITaskTagRepository, TaskTagRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Services
builder.Services.AddScoped<ITaskService, TaskService>();
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

// JWT Authentication Configuration
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
			ClockSkew = TimeSpan.Zero // Crucial para que expire exactamente al segundo
		};

		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				if (context.Exception is SecurityTokenExpiredException)
				{
					// CORRECCIÓN: Usar el indexador para evitar el ArgumentException
					context.Response.Headers["X-Token-Expired"] = "true";

					// TIP: Si usas CORS, debes exponer la cabecera para que el Frontend (Angular) pueda verla
					context.Response.Headers["Access-Control-Expose-Headers"] = "X-Token-Expired";
				}
				return System.Threading.Tasks.Task.CompletedTask;
			}
		};
	});

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"))
    .AddPolicy("DeveloperOrHigher", policy => policy.RequireRole("Admin", "Manager", "Developer"));

builder.Services.AddCors(options => {
	options.AddPolicy("AllowAll", policy => {
		policy.AllowAnyOrigin()
			  .AllowAnyHeader()
			  .WithMethods("GET", "POST", "PUT", "DELETE"); // ¡Asegúrate de que PUT esté aquí!
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Middleware de manejo de excepciones global
app.UseGlobalExceptionHandler();

app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Authentication and Authorization middleware
app.UseAuthentication();
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

// Health Check endpoint at root
app.MapHealthChecks("/health");

// Root endpoint
app.MapGet("/", () => new
{
    service = "TaskFlow API",
    version = "1.0.0",
    status = "running",
    timestamp = DateTime.UtcNow
});

app.MapControllers();

app.Run();
