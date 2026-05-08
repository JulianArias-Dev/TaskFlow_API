namespace TaskFlow_API.Models
{
    public class Tag
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        /// <summary>Descripción opcional para distinguir etiquetas con nombres parecidos.</summary>
        public string? Description { get; set; }

        /// <summary>Color hexadecimal usado por el frontend para badges/pills.</summary>
        public string Color { get; set; } = "#3498db";

        // Foreign Key
        public Guid ProjectId { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Project? Project { get; set; }

        /// <summary>Relación N:M Tag &lt;-&gt; Task vía la tabla intermedia `TaskLabels`.</summary>
        public List<TaskLabel> TaskLabels { get; set; } = new();
    }
}
