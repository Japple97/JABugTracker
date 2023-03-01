using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class ProjectPriority
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }
    }
}