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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User Configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasMany(u => u.OwnedProjects)
            .WithOne(p => p.Owner)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<User>()
            .HasMany(u => u.AssignedTasks)
            .WithOne(t => t.AssignedTo)
            //.HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<User>()
            .HasMany(u => u.Comments)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.ProjectMemberships)
            .WithOne(pm => pm.User)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

		// Project Configuration
		modelBuilder.Entity<Project>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Boards)
            .WithOne(b => b.Project)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Tags)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Members)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Board Configuration
        modelBuilder.Entity<Board>()
            .HasKey(b => b.Id);
        modelBuilder.Entity<Board>()
            .HasMany(b => b.Columns)
            .WithOne(c => c.Board)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Column Configuration
        modelBuilder.Entity<Column>()
            .HasKey(c => c.Id);
        modelBuilder.Entity<Column>()
            .HasMany(c => c.Tasks)
            .WithOne(t => t.Column)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Restrict);

        // Task Configuration
        modelBuilder.Entity<TaskEntity>()
            .HasKey(t => t.Id);
        
        // Recursividad: Task puede tener un ParentTask y múltiples SubTasks
        modelBuilder.Entity<TaskEntity>()
            .HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict para evitar eliminar padres con hijos

        modelBuilder.Entity<TaskEntity>()
            .HasMany(t => t.Comments)
            .WithOne(c => c.Task)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskEntity>()
            .HasMany(t => t.Files)
            .WithOne(f => f.Task)
            .HasForeignKey(f => f.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskEntity>()
            .HasMany(t => t.TaskTags)
            .WithOne(tt => tt.Task)
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

		// Configuración de la relación N:M para Responsables
		modelBuilder.Entity<TaskAssignment>()
			.HasKey(ta => new { ta.TaskId, ta.UserId }); // Clave primaria compuesta

		modelBuilder.Entity<TaskAssignment>()
			.HasOne(ta => ta.Task)
			.WithMany(t => t.Assignments)
			.HasForeignKey(ta => ta.TaskId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<TaskAssignment>()
			.HasOne(ta => ta.User)
			.WithMany() // O .WithMany(u => u.AssignedTasks) si añades la lista en User
			.HasForeignKey(ta => ta.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		// Comment Configuration
		modelBuilder.Entity<Comment>()
            .HasKey(c => c.Id);

        // File Configuration
        modelBuilder.Entity<FileEntity>()
            .HasKey(f => f.Id);
        modelBuilder.Entity<FileEntity>()
            .HasOne(f => f.UploadedBy)
            .WithMany()
            .HasForeignKey(f => f.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Tag Configuration
        modelBuilder.Entity<Tag>()
            .HasKey(t => t.Id);

        // TaskTag Configuration (Relación N:M)
        modelBuilder.Entity<TaskTag>()
            .HasKey(tt => tt.Id);
        modelBuilder.Entity<TaskTag>()
            .HasOne(tt => tt.Task)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskTag>()
            .HasOne(tt => tt.Tag)
            .WithMany(t => t.TaskTags)
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProjectMember Configuration (Relación N:M con información adicional)
        modelBuilder.Entity<ProjectMember>()
            .HasKey(pm => pm.Id);
        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AuditLog Configuration (Sin FK estrictas)
        modelBuilder.Entity<AuditLog>()
            .HasKey(al => al.Id);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.EntityType);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.EntityId);
        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => al.CreatedAt);
    }
}

