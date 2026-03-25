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
    public DbSet<TaskTag> TaskTags { get; set; } = null!;
    public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
	public DbSet<TaskAssignment> TaskAssignments { get; set; } = null!;

	// DbSets de Catálogos (Nuevas Tablas)
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
			// 1. Identificación y Claves
			entity.HasKey(u => u.Id);
			entity.HasIndex(u => u.Email).IsUnique();

			// 2. Propiedades y Valores por Defecto
			entity.Property(u => u.AllowEmail)
				  .HasDefaultValue(true);

			entity.Property(u => u.LightTheme)
				  .HasDefaultValue(true);

			// 3. Relación con el Catálogo de Roles de Aplicación (Admin, CommonUser)
			entity.HasOne(u => u.AppRole)
				  .WithMany() // No necesitamos una lista de usuarios en la tabla AppRole
				  .HasForeignKey(u => u.AppRoleId)
				  .OnDelete(DeleteBehavior.Restrict); // No borrar un rol si tiene usuarios

			// 4. Relaciones de Propiedad (Proyectos que el usuario creó)
			entity.HasMany(u => u.OwnedProjects)
				  .WithOne(p => p.Owner)
				  .HasForeignKey(p => p.OwnerId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 5. Relaciones de Participación (Membresías a proyectos)
			entity.HasMany(u => u.ProjectMemberships)
				  .WithOne(pm => pm.User)
				  .HasForeignKey(pm => pm.UserId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 6. Relaciones de Interacción (Comentarios y Notificaciones)
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
			// 1. Identificación
			entity.HasKey(p => p.Id);

			// 2. Relación con el Catálogo de Estados (Active, Archived, etc.)
			entity.HasOne(p => p.Status)
				  .WithMany() // No necesitamos la lista de proyectos en la tabla de estados
				  .HasForeignKey(p => p.StatusId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 3. Relación con el Propietario (User)
			entity.HasOne(p => p.Owner)
				  .WithMany(u => u.OwnedProjects)
				  .HasForeignKey(p => p.OwnerId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 4. Tableros (Boards) - Eliminación en cascada
			entity.HasMany(p => p.Boards)
				  .WithOne(b => b.Project)
				  .HasForeignKey(b => b.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 5. Etiquetas del Proyecto (Tags) - Eliminación en cascada
			entity.HasMany(p => p.Tags)
				  .WithOne(t => t.Project)
				  .HasForeignKey(t => t.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 6. Miembros del Proyecto (ProjectMembers) - Eliminación en cascada
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

			// 2. Relación con el Proyecto (Navegación inversa)
			entity.HasOne(b => b.Project)
				  .WithMany(p => p.Boards)
				  .HasForeignKey(b => b.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 3. Relación con sus Columnas
			entity.HasMany(b => b.Columns)
				  .WithOne(c => c.Board)
				  .HasForeignKey(c => c.BoardId)
				  .OnDelete(DeleteBehavior.Cascade); // Si borras el tablero, mueren sus columnas
		});

		// Column Configuration
		modelBuilder.Entity<Column>(entity =>
		{
			// 1. Identificación
			entity.HasKey(c => c.Id);

			// 2. Relación con el Tablero (Board)
			entity.HasOne(c => c.Board)
				  .WithMany(b => b.Columns)
				  .HasForeignKey(c => c.BoardId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se elimina el tablero, se eliminan sus columnas

			// 3. Relación con sus Tareas (Tasks)
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
			// 1. Identificación
			entity.HasKey(t => t.Id);

			// 2. Relaciones con Catálogos (Nuevas tablas dinámicas)
			// Usamos Restrict porque no queremos que se borre una "Prioridad" o "Estado" 
			// si todavía hay tareas que los usan.
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

			// 4. Relación con la Columna
			entity.HasOne(t => t.Column)
				  .WithMany(c => c.Tasks)
				  .HasForeignKey(t => t.ColumnId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 5. Colecciones e Interacciones (Eliminación en Cascada)
			entity.HasMany(t => t.Comments)
				  .WithOne(c => c.Task)
				  .HasForeignKey(c => c.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(t => t.Files)
				  .WithOne(f => f.Task)
				  .HasForeignKey(f => f.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(t => t.TaskTags)
				  .WithOne(tt => tt.Task)
				  .HasForeignKey(tt => tt.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 6. Relación con Responsables (N:M a través de TaskAssignment)
			entity.HasMany(t => t.Assignments)
				  .WithOne(ta => ta.Task)
				  .HasForeignKey(ta => ta.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);
		});

		// Configuración de la relación N:M para Responsables
		modelBuilder.Entity<TaskAssignment>(entity =>
		{
			// 1. Clave Primaria Compuesta
			entity.HasKey(ta => new { ta.TaskId, ta.UserId });

			// 2. Relación con la Tarea (Task)
			entity.HasOne(ta => ta.Task)
				  .WithMany(t => t.Assignments)
				  .HasForeignKey(ta => ta.TaskId)
				  .OnDelete(DeleteBehavior.Cascade);

			// 3. Relación con el Usuario (User)
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
			// 1. Identificación
			entity.HasKey(c => c.Id);

			// 2. Contenido y Auditoría
			entity.Property(c => c.Content)
				  .IsRequired()
				  .HasMaxLength(1000); // Limite razonable para comentarios

			entity.Property(c => c.CreatedAt)
				  .HasDefaultValueSql("GETUTCDATE()");

			// 3. Relación con el Autor (User)
			entity.HasOne(c => c.User)
				  .WithMany(u => u.Comments)
				  .HasForeignKey(c => c.UserId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borran sus comentarios

			// 4. Relación con la Tarea (Task)
			entity.HasOne(c => c.Task)
				  .WithMany(t => t.Comments)
				  .HasForeignKey(c => c.TaskId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra la tarea, se borra el hilo de comentarios
		});

		// File Configuration
		modelBuilder.Entity<FileEntity>(entity =>
		{
			// 1. Identificación
			entity.HasKey(f => f.Id);

			// 2. Metadatos del Archivo
			entity.Property(f => f.FileName)
				  .IsRequired()
				  .HasMaxLength(255);

			entity.Property(f => f.FileUrl)
				  .IsRequired();

			entity.Property(f => f.CreatedAt)
				  .HasDefaultValueSql("GETUTCDATE()");

			// 3. Relación con la Tarea (Task)
			entity.HasOne(f => f.Task)
				  .WithMany(t => t.Files)
				  .HasForeignKey(f => f.TaskId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra la tarea, se borran sus adjuntos

			// 4. Relación con el Autor de la subida (User)
			entity.HasOne(f => f.UploadedBy)
				  .WithMany() // No necesitamos la lista de archivos dentro del modelo User
				  .HasForeignKey(f => f.UploadedByUserId)
				  .OnDelete(DeleteBehavior.SetNull); // Mantenemos el archivo aunque el usuario ya no exista
		});

		// Tag Configuration
		modelBuilder.Entity<Tag>(entity =>
		{
			// 1. Identificación
			entity.HasKey(t => t.Id);

			// 2. Propiedades de la Etiqueta
			entity.Property(t => t.Name)
				  .IsRequired()
				  .HasMaxLength(50);

			entity.Property(t => t.Color)
				  .HasMaxLength(7); // Para guardar el Hexadecimal (ej: #FF5733)

			// 3. Relación con el Proyecto
			entity.HasOne(t => t.Project)
				  .WithMany(p => p.Tags)
				  .HasForeignKey(t => t.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el proyecto, se borran sus etiquetas
		});

		// TaskTag Configuration (Relación N:M)
		modelBuilder.Entity<TaskTag>(entity =>
		{
			// 1. Identificación
			entity.HasKey(tt => tt.Id);

			// 2. Relación con la Tarea
			entity.HasOne(tt => tt.Task)
				  .WithMany(t => t.TaskTags)
				  .HasForeignKey(tt => tt.TaskId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra la tarea, se quita la relación con el tag

			// 3. Relación con la Etiqueta (Tag)
			entity.HasOne(tt => tt.Tag)
				  .WithMany(t => t.TaskTags)
				  .HasForeignKey(tt => tt.TagId)
				  .OnDelete(DeleteBehavior.Restrict); // No borrar el Tag si está en uso
		});

		// ProjectMember Configuration (Relación N:M con información adicional)
		modelBuilder.Entity<ProjectMember>(entity =>
		{
			// 1. Identificación
			entity.HasKey(pm => pm.Id);

			// 2. Relación con el Catálogo de Roles de Proyecto (Creator, PM, Developer)
			// Usamos Restrict para no poder borrar un "Rol" si hay gente asignada a él.
			entity.HasOne(pm => pm.ProjectRole)
				  .WithMany() // No necesitamos la lista de miembros en la tabla ProjectRole
				  .HasForeignKey(pm => pm.ProjectRoleId)
				  .OnDelete(DeleteBehavior.Restrict);

			// 3. Relación con el Proyecto
			entity.HasOne(pm => pm.Project)
				  .WithMany(p => p.Members)
				  .HasForeignKey(pm => pm.ProjectId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el proyecto, se borran las membresías

			// 4. Relación con el Usuario (User)
			entity.HasOne(pm => pm.User)
				  .WithMany(u => u.ProjectMemberships)
				  .HasForeignKey(pm => pm.UserId)
				  .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, sale de todos los proyectos

			// 5. Auditoría de Membresía
			entity.Property(pm => pm.JoinedAt)
				  .HasDefaultValueSql("GETUTCDATE()");
		});


		// AuditLog Configuration (Sin FK estrictas)
		modelBuilder.Entity<AuditLog>(entity =>
		{
			// 1. Identificación
			entity.HasKey(al => al.Id);

			// 2. Índices para Optimización de Consultas
			// Estos son críticos porque la tabla de auditoría suele crecer mucho
			entity.HasIndex(al => al.EntityType);
			entity.HasIndex(al => al.EntityId);
			entity.HasIndex(al => al.CreatedAt);
			entity.HasIndex(al => al.UserId); // Recomendado para filtrar por "quién hizo qué"

			// 3. Configuración de Propiedades
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

			// 4. Relación con el Usuario (Opcional, pero útil)
			entity.HasOne(al => al.User)
				  .WithMany()
				  .HasForeignKey(al => al.UserId)
				  .OnDelete(DeleteBehavior.SetNull); // Si el usuario se borra, el log permanece
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

			// Relación Uno a Muchos (Un usuario tiene muchas notificaciones)
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
			new AppRole { Id = 1, Name = "Admin" },
			new AppRole { Id = 2, Name = "CommonUser" }
		);

		// ProjectRoles
		modelBuilder.Entity<ProjectRole>().HasData(
			new ProjectRole { Id = 1, Name = "Creator" },
			new ProjectRole { Id = 2, Name = "Project Manager" },
			new ProjectRole { Id = 3, Name = "Developer" }
		);

		// TaskPriority
		modelBuilder.Entity<TaskPriority>().HasData(
			new TaskPriority { Id = 1, Name = "LOW" },
			new TaskPriority { Id = 2, Name = "MEDIUM" },
			new TaskPriority { Id = 3, Name = "HIGH" },
			new TaskPriority { Id = 4, Name = "CRITICAL" }
		);

        modelBuilder.Entity<TaskFlow_API.Models.TaskStatus>().HasData(
            new TaskFlow_API.Models.TaskStatus { Id = 1, Name = "To Do" },
            new TaskFlow_API.Models.TaskStatus { Id = 2, Name = "In Progress" },
            new TaskFlow_API.Models.TaskStatus { Id = 3, Name = "Done" },
            new TaskFlow_API.Models.TaskStatus { Id = 4, Name = "Blocked" }
        );

        modelBuilder.Entity<TaskType>().HasData(
            new TaskType { Id = 1, Name = "Feature" },
            new TaskType { Id = 2, Name = "Bug" },
            new TaskType { Id = 3, Name = "Improvement" },
            new TaskType { Id = 4, Name = "Research" },
			new TaskType { Id = 5, Name = "Task" },
			new TaskType { Id = 6, Name = "SubTask" }
		);

        modelBuilder.Entity<ProjectStatus>().HasData(
            new ProjectStatus { Id = 1, Name = "Active" },
            new ProjectStatus { Id = 2, Name = "Completed" },
            new ProjectStatus { Id = 3, Name = "On Hold" },
            new ProjectStatus { Id = 4, Name = "Cancelled" }
        );  		
	}
}

