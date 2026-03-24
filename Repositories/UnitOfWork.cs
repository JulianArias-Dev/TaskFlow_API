using TaskFlow_API.Data;

namespace TaskFlow_API.Repositories;

/// <summary>
/// Interfaz del patrón Unit of Work
/// Coordina múltiples repositorios y gestiona transacciones
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ITaskRepository Tasks { get; }
    IProjectRepository Projects { get; }
    IUserRepository Users { get; }
    IBoardRepository Boards { get; }
    IColumnRepository Columns { get; }
    ICommentRepository Comments { get; }
    IFileRepository Files { get; }
    ITagRepository Tags { get; }
    ITaskTagRepository TaskTags { get; }
    IProjectMemberRepository ProjectMembers { get; }
    IAuditLogRepository AuditLogs { get; }

    System.Threading.Tasks.Task<int> SaveChangesAsync();
    System.Threading.Tasks.Task<bool> BeginTransactionAsync();
    System.Threading.Tasks.Task<bool> CommitTransactionAsync();
    System.Threading.Tasks.Task<bool> RollbackTransactionAsync();
}

/// <summary>
/// Implementación del patrón Unit of Work
/// Proporciona una forma centralizada de acceder a todos los repositorios
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TaskFlowDbContext _context;
    
    private ITaskRepository? _taskRepository;
    private IProjectRepository? _projectRepository;
    private IUserRepository? _userRepository;
    private IBoardRepository? _boardRepository;
    private IColumnRepository? _columnRepository;
    private ICommentRepository? _commentRepository;
    private IFileRepository? _fileRepository;
    private ITagRepository? _tagRepository;
    private ITaskTagRepository? _taskTagRepository;
    private IProjectMemberRepository? _projectMemberRepository;
    private IAuditLogRepository? _auditLogRepository;

    public UnitOfWork(TaskFlowDbContext context)
    {
        _context = context;
    }

    public ITaskRepository Tasks => _taskRepository ??= new TaskRepository(_context);
    public IProjectRepository Projects => _projectRepository ??= new ProjectRepository(_context);
    public IUserRepository Users => _userRepository ??= new UserRepository(_context);
    public IBoardRepository Boards => _boardRepository ??= new BoardRepository(_context);
    public IColumnRepository Columns => _columnRepository ??= new ColumnRepository(_context);
    public ICommentRepository Comments => _commentRepository ??= new CommentRepository(_context);
    public IFileRepository Files => _fileRepository ??= new FileRepository(_context);
    public ITagRepository Tags => _tagRepository ??= new TagRepository(_context);
    public ITaskTagRepository TaskTags => _taskTagRepository ??= new TaskTagRepository(_context);
    public IProjectMemberRepository ProjectMembers => _projectMemberRepository ??= new ProjectMemberRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogRepository ??= new AuditLogRepository(_context);

    public async System.Threading.Tasks.Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async System.Threading.Tasks.Task<bool> BeginTransactionAsync()
    {
        try
        {
            await _context.Database.BeginTransactionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async System.Threading.Tasks.Task<bool> CommitTransactionAsync()
    {
        try
        {
            await _context.Database.CommitTransactionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async System.Threading.Tasks.Task<bool> RollbackTransactionAsync()
    {
        try
        {
            await _context.Database.RollbackTransactionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
