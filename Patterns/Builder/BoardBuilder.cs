using TaskFlow_API.Models;

namespace TaskFlow_API.Patterns.Builder
{
	public class BoardBuilder
	{
		private readonly Board _board;

		public BoardBuilder()
		{
			_board = new Board();
		}

		public BoardBuilder WithBasicInfo(string name, string description, Guid projectId)
		{
			_board.Name = name;
			_board.Description = description;
			_board.ProjectId = projectId;
			return this;
		}

		public BoardBuilder WithDefaultColumns()
		{
			_board.Columns = new List<Column>
		{
			new Column { Name = "Por hacer", DisplayOrder = 0, Color = "#E2E8F0" },
			new Column { Name = "En progreso", DisplayOrder = 1, Color = "#BEE3F8" },
			new Column { Name = "En revisión", DisplayOrder = 2, Color = "#FEFCBF" },
			new Column { Name = "Finalizado", DisplayOrder = 3, Color = "#C6F6D5" }
		};
			return this;
		}

		public Board Build() => _board;
	}
}
