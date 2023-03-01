using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class TicketStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }
    }
}
