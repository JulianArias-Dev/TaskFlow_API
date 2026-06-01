using Microsoft.EntityFrameworkCore;
using TaskFlow_API.Models;
using TaskEntity = TaskFlow_API.Models.Task;
using FileEntity = TaskFlow_API.Models.File;

namespace TaskFlow_API.Data;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options) { }

    // DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Board> Boards { get; set; } = null!;
    public DbSet<Column> Columns { get; set; } = null!;
    public DbSet<TaskEntity> Tasks { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<FileEntity> Files { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<TaskLabel> TaskLabels { get; set; } = null!;
    public DbSet<PasswordPolicy> PasswordPolicies { get; set; } = null!;
    public DbSet<AttachmentPolicy> AttachmentPolicies { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
	public DbSet<TaskAssignment> TaskAssignments { get; set; } = null!;
	public DbSet<RevokedToken> RevokedTokens { get; set; } = null!;
	public DbSet<SavedFilter> SavedFilters { get; set; } = null!;

	// DbSets de Cat�logos (Nuevas Tablas)
	public DbSet<TaskPriority> TaskPriorities { get; set; } = null!;
	public DbSet<TaskFlow_API.Models.TaskStatus> TaskStatuses { get; set; } = null!;
	public DbSet<TaskType> TaskTypes { get; set; } = null!;
	public DbSet<ProjectStatus> ProjectStatuses { get; set; } = null!;
	public DbSet<AppRole> AppRoles { get; set; } = null!;
	public DbSet<ProjectRole> ProjectRoles { get; set; } = null!;


	protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

		ConfigureCatalogs(modelBuilder);

		// User Configuration
		modelBuilder.Entity<User>(entity =>
		{
			// 1. Identificaci�n y Claves
			entity.HasKey(u => u.Id);
			entity.HasIndex(u => u.Email).IsUnique();

			// 2. Propiedades y Valores por Defecto
			entity.Property(u => u.NotifyByEmail)
				  .HasDefaultValue(true);

			// ThemePreference: persistido como string ("Light" / "Dark" / "System")
			// para que el cliente Angular/React pueda alimentar su Abstract Factory de UI sin mapeo extra.
			entity.Property(u => u.ThemePreference)
				  .HasConversion<string>()
				  .HasMaxLength(20)
				  .HasDefaultValue(ThemePreference.Light);

			// Preferencias por evento (RF-05.3) — JSON crudo. Usamos nvarchar(max)
			// para no atarnos a una estructura concreta; el formato se valida en
			// la capa de servicio.
			entity.Property(u => u.NotificationPreferences)
				  .HasColumnType("nvarchar(max)");

			// 3. Relaci�n con el Cat�logo de Roles de Aplicaci�n (Admin, CommonUser)
			entity.HasOne(u => u.AppRole)
				  .WithMany() // No necesitamos una lista de usuarios en la tabla AppRole
				  .HasForeignKey(u => u.AppRoleId)
				  .OnDelete(DeleteBehavior.Restrict); // No borrar un rol si tiene usuarios

			// 4. Relaciones de Propiedad (Proyectos que el usuario cre�)
			entity.HasMany(u => u.OwnedProjects)
				  .WithOne(p => p.Owner)
				  .HasForeignKey(p => p.OwnerId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 5. Relaciones de Participaci�n (Membres�as a proyectos)
			entity.HasMany(u => u.ProjectMemberships)
				  .WithOne(pm => pm.User)
				  .HasForeignKey(pm => pm.UserId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 6. Relaciones de Interacci�n (Comentarios y Notificaciones)
			entity.HasMany(u => u.Comments)
				  .WithOne(c => c.User)
				  .HasForeignKey(c => c.UserId)
				  .OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(u => u.Notifications)
				  .WithOne(n => n.User)
				  .HasForeignKey(n => n.UserId)
				  .OnDelete(DeleteBehavior.Cascade);
		});

		// Project Configuration
		modelBuilder.Entity<Project>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(p => p.Id);

			// 2. Relaci�n con el Cat�logo de Estados (Active, Archived, etc.)
			entity.HasOne(p => p.Status)
				  .WithMany() // No necesitamos la lista de proyectos en la tabla de estados
				  .HasForeignKey(p => p.StatusId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 3. Relaci�n con el Propietario (User)
			entity.HasOne(p => p.Owner)
				  .WithMany(u => u.OwnedProjects)
				  .HasForeignKey(p => p.OwnerId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 4. Tableros (Boards) - Eliminaci�n en cascada
			entity.HasMany(p => p.Boards)
				  .WithOne(b => b.Project)
				  .HasForeignKey(b => b.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 5. Etiquetas del Proyecto (Tags) - Eliminaci�n en cascada
			entity.HasMany(p => p.Tags)
				  .WithOne(t => t.Project)
				  .HasForeignKey(t => t.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 6. Miembros del Proyecto (ProjectMembers) - Eliminaci�n en cascada
			entity.HasMany(p => p.Members)
				  .WithOne(pm => pm.Project)
				  .HasForeignKey(pm => pm.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);
		});

		// Board Configuration
		modelBuilder.Entity<Board>(entity =>
		{
			// 1. Clave Primaria
			entity.HasKey(b => b.Id);

			// 2. Relaci�n con el Proyecto (Navegaci�n inversa)
			entity.HasOne(b => b.Project)
				  .WithMany(p => p.Boards)
				  .HasForeignKey(b => b.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 3. Relaci�n con sus Columnas
			entity.HasMany(b => b.Columns)
				  .WithOne(c => c.Board)
				  .HasForeignKey(c => c.BoardId)
				  .OnDelete(DeleteBehavior.Cascade); // Si borras el tablero, mueren sus columnas
		});

		// Column Configuration
		modelBuilder.Entity<Column>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(c => c.Id);

			// 2. Relaci�n con el Tablero (Board)
			entity.HasOne(c => c.Board)
				  .WithMany(b => b.Columns)
				  .HasForeignKey(c => c.BoardId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se elimina el tablero, se eliminan sus columnas

			// 3. Relaci�n con sus Tareas (Tasks)
			entity.HasMany(c => c.Tasks)
				  .WithOne(t => t.Column)
				  .HasForeignKey(t => t.ColumnId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se elimina la columna, se eliminan sus tareas

			// 4. Propiedades adicionales (Opcional, pero recomendado)
			entity.Property(c => c.Name)
				  .IsRequired()
				  .HasMaxLength(100);

			entity.Property(c => c.DisplayOrder)
				  .HasDefaultValue(0);
		});

		// Task Configuration
		modelBuilder.Entity<TaskEntity>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(t => t.Id);

			// 2. Relaciones con Cat�logos (Nuevas tablas din�micas)
			// Usamos Restrict porque no queremos que se borre una "Prioridad" o "Estado" 
			// si todav�a hay tareas que los usan.
			entity.HasOne(t => t.Priority)
				  .WithMany()
				  .HasForeignKey(t => t.PriorityId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(t => t.Status)
				  .WithMany()
				  .HasForeignKey(t => t.StatusId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(t => t.Type)
				  .WithMany()
				  .HasForeignKey(t => t.TypeId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 3. Recursividad (Subtareas)
			entity.HasOne(t => t.ParentTask)
				  .WithMany(t => t.SubTasks)
				  .HasForeignKey(t => t.ParentTaskId)
				  .OnDelete(DeleteBehavior.Restrict); // Evita borrar una tarea padre si tiene hijos

			// 4. Relaci�n con la Columna
			entity.HasOne(t => t.Column)
				  .WithMany(c => c.Tasks)
				  .HasForeignKey(t => t.ColumnId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 5. Colecciones e Interacciones (Eliminaci�n en Cascada)
			entity.HasMany(t => t.Comments)
				  .WithOne(c => c.Task)
				  .HasForeignKey(c => c.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(t => t.Files)
				  .WithOne(f => f.Task)
				  .HasForeignKey(f => f.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(t => t.TaskLabels)
				  .WithOne(tl => tl.Task)
				  .HasForeignKey(tl => tl.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 6. Relaci�n con Responsables (N:M a trav�s de TaskAssignment)
			entity.HasMany(t => t.Assignments)
				  .WithOne(ta => ta.Task)
				  .HasForeignKey(ta => ta.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);
		});

		// Configuraci�n de la relaci�n N:M para Responsables
		modelBuilder.Entity<TaskAssignment>(entity =>
		{
			// 1. Clave Primaria Compuesta
			entity.HasKey(ta => new { ta.TaskId, ta.UserId });

			// 2. Relaci�n con la Tarea (Task)
			entity.HasOne(ta => ta.Task)
				  .WithMany(t => t.Assignments)
				  .HasForeignKey(ta => ta.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 3. Relaci�n con el Usuario (User)
			entity.HasOne(ta => ta.User)
				  .WithMany(u => u.Assignments)
				  .HasForeignKey(ta => ta.UserId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 4. Propiedades Adicionales
			entity.Property(ta => ta.AssignedAt)
				  .HasDefaultValueSql("GETUTCDATE()");
		});

		// Comment Configuration
		modelBuilder.Entity<Comment>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(c => c.Id);

			// 2. Contenido y Auditor�a
			entity.Property(c => c.Content)
				  .IsRequired()
				  .HasMaxLength(1000); // Limite razonable para comentarios

			entity.Property(c => c.CreatedAt)
				  .HasDefaultValueSql("GETUTCDATE()");

			// 3. Relaci�n con el Autor (User)
			entity.HasOne(c => c.User)
				  .WithMany(u => u.Comments)
				  .HasForeignKey(c => c.UserId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borran sus comentarios

			// 4. Relaci�n con la Tarea (Task)
			entity.HasOne(c => c.Task)
				  .WithMany(t => t.Comments)
				  .HasForeignKey(c => c.TaskId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra la tarea, se borra el hilo de comentarios
		});

		// File Configuration
		modelBuilder.Entity<FileEntity>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(f => f.Id);

			// 2. Metadatos del Archivo
			entity.Property(f => f.FileName)
				  .IsRequired()
				  .HasMaxLength(255);

			entity.Property(f => f.FileUrl)
				  .IsRequired();

			entity.Property(f => f.CreatedAt)
				  .HasDefaultValueSql("GETUTCDATE()");

			// 3. Relaci�n con la Tarea (Task)
			entity.HasOne(f => f.Task)
				  .WithMany(t => t.Files)
				  .HasForeignKey(f => f.TaskId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra la tarea, se borran sus adjuntos

			// 4. Relaci�n con el Autor de la subida (User)
			entity.HasOne(f => f.UploadedBy)
				  .WithMany() // No necesitamos la lista de archivos dentro del modelo User
				  .HasForeignKey(f => f.UploadedByUserId)
				  .OnDelete(DeleteBehavior.SetNull); // Mantenemos el archivo aunque el usuario ya no exista
		});

		// Tag Configuration
		modelBuilder.Entity<Tag>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(t => t.Id);

			// 2. Propiedades de la Etiqueta
			entity.Property(t => t.Name)
				  .IsRequired()
				  .HasMaxLength(50);

			entity.Property(t => t.Color)
				  .HasMaxLength(7); // Para guardar el Hexadecimal (ej: #FF5733)

			entity.Property(t => t.Description)
				  .HasMaxLength(250);

			// 3. Relaci�n con el Proyecto
			entity.HasOne(t => t.Project)
				  .WithMany(p => p.Tags)
				  .HasForeignKey(t => t.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el proyecto, se borran sus etiquetas
		});

		// TaskLabel — tabla intermedia que rompe la relación N:M Task <-> Tag
		modelBuilder.Entity<TaskLabel>(entity =>
		{
			entity.ToTable("TaskLabels");

			// 1. PK compuesta — evita duplicados (TaskId + TagId única).
			entity.HasKey(tl => new { tl.TaskId, tl.TagId });

			// 2. FK a Task con cascade (si se borra la tarea, sus labels también).
			entity.HasOne(tl => tl.Task)
				  .WithMany(t => t.TaskLabels)
				  .HasForeignKey(tl => tl.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 3. FK a Tag con restrict (no permitir borrar un Tag en uso).
			entity.HasOne(tl => tl.Tag)
				  .WithMany(t => t.TaskLabels)
				  .HasForeignKey(tl => tl.TagId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 4. Auditoría — fecha en la que se asignó la etiqueta a la tarea.
			entity.Property(tl => tl.AssignedAt)
				  .HasDefaultValueSql("GETUTCDATE()");
		});

		// PasswordPolicy — política global del sistema (singleton lógico).
		modelBuilder.Entity<PasswordPolicy>(entity =>
		{
			entity.ToTable("PasswordPolicies");
			entity.HasKey(p => p.PolicyId);
			entity.Property(p => p.PolicyId).ValueGeneratedOnAdd();

			entity.Property(p => p.MinLength).HasDefaultValue(8);
			entity.Property(p => p.MaxLength).HasDefaultValue(64);
			entity.Property(p => p.MinUpperChars).HasDefaultValue(1);
			entity.Property(p => p.MinSpecialChars).HasDefaultValue(1);
			entity.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

			// Seed — fila por defecto creada en la migración inicial.
			entity.HasData(new PasswordPolicy
			{
				PolicyId = 1,
				MinLength = 8,
				MaxLength = 64,
				MinUpperChars = 1,
				MinSpecialChars = 1,
				UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			});
		});

		// AttachmentPolicy — política global para adjuntos.
		modelBuilder.Entity<AttachmentPolicy>(entity =>
		{
			entity.ToTable("AttachmentPolicies");
			entity.HasKey(p => p.PolicyId);
			entity.Property(p => p.PolicyId).ValueGeneratedOnAdd();

			entity.Property(p => p.MaxSizeMB).HasDefaultValue(10);
			entity.Property(p => p.MaxFilesByTask).HasDefaultValue(20);
			entity.Property(p => p.AllowedFormats)
				  .IsRequired()
				  .HasMaxLength(500);
			entity.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

			entity.HasData(new AttachmentPolicy
			{
				PolicyId = 1,
				MaxSizeMB = 10,
				MaxFilesByTask = 20,
				AllowedFormats = "pdf,png,jpg,jpeg,gif,docx,xlsx,pptx,txt,zip",
				UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			});
		});

		// ProjectMember Configuration (Relaci�n N:M con informaci�n adicional)
		modelBuilder.Entity<ProjectMember>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(pm => pm.Id);

			// 2. Relaci�n con el Cat�logo de Roles de Proyecto (Creator, PM, Developer)
			// Usamos Restrict para no poder borrar un "Rol" si hay gente asignada a �l.
			entity.HasOne(pm => pm.ProjectRole)
				  .WithMany() // No necesitamos la lista de miembros en la tabla ProjectRole
				  .HasForeignKey(pm => pm.ProjectRoleId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 3. Relaci�n con el Proyecto
			entity.HasOne(pm => pm.Project)
				  .WithMany(p => p.Members)
				  .HasForeignKey(pm => pm.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el proyecto, se borran las membres�as

			// 4. Relaci�n con el Usuario (User)
			entity.HasOne(pm => pm.User)
				  .WithMany(u => u.ProjectMemberships)
				  .HasForeignKey(pm => pm.UserId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, sale de todos los proyectos

			// 5. Auditor�a de Membres�a
			entity.Property(pm => pm.JoinedAt)
				  .HasDefaultValueSql("GETUTCDATE()");
		});


		// AuditLog Configuration (Sin FK estrictas)
		modelBuilder.Entity<AuditLog>(entity =>
		{
			// 1. Identificaci�n
			entity.HasKey(al => al.Id);

			// 2. �ndices para Optimizaci�n de Consultas
			// Estos son cr�ticos porque la tabla de auditor�a suele crecer mucho
			entity.HasIndex(al => al.EntityType);
			entity.HasIndex(al => al.EntityId);
			entity.HasIndex(al => al.CreatedAt);
			entity.HasIndex(al => al.UserId); // Recomendado para filtrar por "qui�n hizo qu�"

			// 3. Configuraci�n de Propiedades
			entity.Property(al => al.Action)
				  .IsRequired()
				  .HasMaxLength(100); // Ej: "CREATE", "UPDATE", "DELETE"

			entity.Property(al => al.EntityType)
				  .IsRequired()
				  .HasMaxLength(100); // Ej: "Task", "Project"

			entity.Property(al => al.EntityId)
				  .IsRequired();

			entity.Property(al => al.NewData)
				  .HasColumnType("nvarchar(max)"); // O "json" si usas PostgreSQL/SQL Server moderno

			entity.Property(al => al.CreatedAt)
				  .HasDefaultValueSql("GETUTCDATE()");

			// 4. Relaci�n con el Usuario (Opcional, pero �til)
			entity.HasOne(al => al.User)
				  .WithMany()
				  .HasForeignKey(al => al.UserId)
				  .OnDelete(DeleteBehavior.SetNull); // Si el usuario se borra, el log permanece
		});

		// RevokedToken Configuration (JWT blacklist)
		modelBuilder.Entity<RevokedToken>(entity =>
		{
			entity.HasKey(rt => rt.Id);
			entity.HasIndex(rt => rt.TokenId).IsUnique();
			entity.HasIndex(rt => rt.ExpiresAt); // útil para purgas
			entity.Property(rt => rt.TokenId)
				  .IsRequired()
				  .HasMaxLength(512);
			entity.Property(rt => rt.Reason).HasMaxLength(200);
			entity.Property(rt => rt.RevokedAt).HasDefaultValueSql("GETUTCDATE()");
		});

		// SavedFilter Configuration (RF-07.3)
		modelBuilder.Entity<SavedFilter>(entity =>
		{
			entity.HasKey(sf => sf.Id);
			entity.Property(sf => sf.Name).IsRequired().HasMaxLength(100);
			entity.Property(sf => sf.FilterCriteria).IsRequired().HasColumnType("nvarchar(max)");
			entity.Property(sf => sf.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
			entity.HasOne(sf => sf.User)
				  .WithMany()
				  .HasForeignKey(sf => sf.UserId)
				  .OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<Notification>(entity =>
		{
			entity.ToTable("Notifications");
			entity.HasKey(n => n.Id);

			entity.Property(n => n.Subject)
				  .IsRequired()
				  .HasMaxLength(200);

			entity.Property(n => n.Content)
				  .IsRequired();

			entity.Property(n => n.CreatedAt)
				  .HasDefaultValueSql("GETUTCDATE()");

			// Relaci�n Uno a Muchos (Un usuario tiene muchas notificaciones)
			entity.HasOne(n => n.User)
				  .WithMany(u => u.Notifications)
				  .HasForeignKey(n => n.UserId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borran sus notificaciones
		});

	}

	private void ConfigureCatalogs(ModelBuilder modelBuilder)
	{
		// AppRoles
		modelBuilder.Entity<AppRole>().HasData(
			new AppRole { Id = 1, Name = "Administrador" },
			new AppRole { Id = 2, Name = "Usuario Común" }
		);

		// ----- SuperAdmin (bootstrap) ---------------------------------------
		// Usuario administrador inicial para demos y operación. Se siembra
		// desde la migración (`HasData`) para que exista desde la primera
		// vez que se levanta la BD, sin necesidad de código en startup.
		//
		// Credenciales:
		//   email:    superadmin@taskflow.com
		//   password: Admin123*
		//
		// El hash BCrypt está pre-computado (cost=11). Para regenerarlo:
		//   BCrypt.Net.BCrypt.HashPassword("Admin123*")
		// y reemplazar la constante de abajo + crear una nueva migración.
		modelBuilder.Entity<User>().HasData(new User
		{
			Id = new Guid("00000000-0000-0000-0000-000000000001"),
			Email = "superadmin@taskflow.com",
			Name = "SuperAdmin",
			PasswordHash = "$2a$11$QiuTL7x1k/8hGeh4GXfhZOT2WD3lkz7S2EYkUS6rMJsAa71X1883m",
			AppRoleId = 1, // Admin
			IsActive = true,
			NotifyByEmail = true,
			ThemePreference = ThemePreference.Light,
			CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
		});

		// ProjectRoles
		modelBuilder.Entity<ProjectRole>().HasData(
			new ProjectRole { Id = 1, Name = "Creador" },
			new ProjectRole { Id = 2, Name = "Gestor de Proyecto" },
			new ProjectRole { Id = 3, Name = "Desarrollador" }
		);

		// TaskPriority
		modelBuilder.Entity<TaskPriority>().HasData(
			new TaskPriority { Id = 1, Name = "Baja" },
			new TaskPriority { Id = 2, Name = "Media" },
			new TaskPriority { Id = 3, Name = "Alta" },
			new TaskPriority { Id = 4, Name = "Crítica" }
		);

        modelBuilder.Entity<TaskFlow_API.Models.TaskStatus>().HasData(
            new TaskFlow_API.Models.TaskStatus { Id = 1, Name = "Por Hacer" },
            new TaskFlow_API.Models.TaskStatus { Id = 2, Name = "En Progreso" },
            new TaskFlow_API.Models.TaskStatus { Id = 3, Name = "Hecho" },
            new TaskFlow_API.Models.TaskStatus { Id = 4, Name = "Bloqueado" }
        );

        modelBuilder.Entity<TaskType>().HasData(
            new TaskType { Id = 1, Name = "Funcionalidad" },
            new TaskType { Id = 2, Name = "Error" },
            new TaskType { Id = 3, Name = "Mejora" },
            new TaskType { Id = 4, Name = "Investigación" },
			new TaskType { Id = 5, Name = "Tarea" },
			new TaskType { Id = 6, Name = "Subtarea" }
		);

        modelBuilder.Entity<ProjectStatus>().HasData(
            new ProjectStatus { Id = 1, Name = "Activo" },
            new ProjectStatus { Id = 2, Name = "Completado" },
            new ProjectStatus { Id = 3, Name = "En Pausa" },
            new ProjectStatus { Id = 4, Name = "Cancelado" },
            new ProjectStatus { Id = 5, Name = "Archivado" }
        );
	}
}

