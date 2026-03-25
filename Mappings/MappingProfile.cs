using AutoMapper;
using TaskFlow_API.DTOs;
using TaskFlow_API.Models;
using TaskEntity = TaskFlow_API.Models.Task;
using FileEntity = TaskFlow_API.Models.File;

namespace TaskFlow_API.Mappings;

/// <summary>
/// Perfil de mapeo AutoMapper para entidades y DTOs
/// Define conversiones bidireccionales automáticas
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Task Mappings
        CreateMap<TaskEntity, TaskDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()))
            .ForMember(dest => dest.SubTaskCount, opt => opt.MapFrom(src => src.SubTasks.Count))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count))
            .ForMember(dest => dest.FileCount, opt => opt.MapFrom(src => src.Files.Count))
            .ReverseMap()
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Priority, opt => opt.Ignore());

        CreateMap<TaskEntity, TaskSimpleDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()));

        CreateMap<CreateTaskDto, TaskEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Priority, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.Files, opt => opt.Ignore())
            .ForMember(dest => dest.SubTasks, opt => opt.Ignore())
            .ForMember(dest => dest.TaskTags, opt => opt.Ignore());

        // Project Mappings
        CreateMap<Project, ProjectDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.MemberIds, opt => opt.MapFrom(src => src.Members.Select(m => m.UserId)))
            .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count))
            .ForMember(dest => dest.BoardCount, opt => opt.MapFrom(src => src.Boards.Count));

        CreateMap<Project, ProjectSimpleDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count));

        CreateMap<CreateProjectDto, Project>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .ForMember(dest => dest.Boards, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.Members, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

		// User Mappings
		// 1. Mapeo para UserDto
		CreateMap<User, UserDto>()
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.AppRole != null ? src.AppRole.Name : "User"))
			.ForMember(dest => dest.ProjectCount, opt => opt.MapFrom(src => src.ProjectMemberships.Count))
			.ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Assignments.Count));

		// 2. Mapeo para UserSimpleDto
		CreateMap<User, UserSimpleDto>()
			// ✅ Mismo cambio aquí para el nombre descriptivo
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.AppRole != null ? src.AppRole.Name : "User"));

		CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Será hasheada en el servicio
            .ForMember(dest => dest.AppRole, opt => opt.Ignore())
            .ForMember(dest => dest.OwnedProjects, opt => opt.Ignore())
            .ForMember(dest => dest.Assignments, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectMemberships, opt => opt.Ignore());

        // Board Mappings
        CreateMap<Board, BoardDto>()
            .ForMember(dest => dest.ColumnCount, opt => opt.MapFrom(src => src.Columns.Count));

        CreateMap<CreateBoardDto, Board>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Columns, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore());

        // Column Mappings
        CreateMap<Column, ColumnDto>()
            .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks.Count))
            .ForMember(dest => dest.Tasks, opt => opt.MapFrom(src => src.Tasks.Select(t => new TaskSimpleDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
				AssignedUserIds = t.Assignments != null
			        ? t.Assignments.Select(a => a.UserId).ToList()
			        : new List<Guid>(),
				DueDate = t.DueDate
            })));

        CreateMap<CreateColumnDto, Column>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .ForMember(dest => dest.Board, opt => opt.Ignore());

        // Comment Mappings
        CreateMap<Comment, CommentDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : "Sistema"))
            .ForMember(dest => dest.UserAvatar, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

        CreateMap<CreateCommentDto, Comment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsEdited, opt => opt.Ignore())
            .ForMember(dest => dest.Task, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // File Mappings
        CreateMap<FileEntity, FileDto>()
            .ForMember(dest => dest.UploadedByUserName, opt => opt.MapFrom(src => src.UploadedBy != null ? src.UploadedBy.Name : "Sistema"));

        // Tag Mappings
        CreateMap<Tag, TagDto>()
            .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.TaskTags.Count));

        CreateMap<CreateTagDto, Tag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.TaskTags, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore());

        // ProjectMember Mappings
        CreateMap<ProjectMember, ProjectMemberDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.Name : ""))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : ""))
            .ForMember(dest => dest.UserAvatar, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null))
			.ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.ProjectRole != null ? src.ProjectRole.Name : "Sev"));

		CreateMap<AddProjectMemberDto, ProjectMember>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.JoinedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectRole, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // AuditLog Mappings
        CreateMap<AuditLog, AuditLogDto>();
    }
}
