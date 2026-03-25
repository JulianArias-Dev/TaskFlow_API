namespace TaskFlow_API.Models
{
	// Base para todos los catálogos
	public abstract class BaseCatalog
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
	}

	public class TaskType : BaseCatalog { }

	public class TaskStatus : BaseCatalog
	{
		public string Color { get; set; } = "#808080";
	}

	public class TaskPriority : BaseCatalog
	{
		public int Level { get; set; } // 1, 2, 3, 4
		public string Color { get; set; } = "#FFFFFF";
	}

	public class ProjectStatus : BaseCatalog { }

	// División de Roles
	public class AppRole : BaseCatalog { } // Admin, CommonUser
	public class ProjectRole : BaseCatalog { } // Creator, PM, Developer
}
