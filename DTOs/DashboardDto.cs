namespace TaskFlow_API.DTOs;

/// <summary>
/// Dashboard centralizado de un proyecto (RF-08.1).
/// Es lo que devuelve el Facade — combina datos de tasks + users + métricas.
/// </summary>
public class DashboardDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectStatus { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Métricas de tareas
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int BlockedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double CompletionPercentage { get; set; }

    // Equipo
    public int MemberCount { get; set; }
    public List<MemberWorkloadDto> MemberWorkloads { get; set; } = new();

    // Velocidad y carga
    public int TotalEstimatedHours { get; set; }
    public int TotalActualHours { get; set; }
    public List<UpcomingTaskDto> UpcomingDeadlines { get; set; } = new();

    public DateTime GeneratedAt { get; set; }
}

public class MemberWorkloadDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int AssignedTasks { get; set; }
    public int CompletedTasks { get; set; }
}

public class UpcomingTaskDto
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int PriorityId { get; set; }
}
