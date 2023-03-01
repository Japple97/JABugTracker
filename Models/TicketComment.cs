using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class TicketComment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        public string? Comment { get; set; }

        public DateTime Created { get; set; }



        // Navigation properties
        public virtual Ticket? Ticket { get; set; }
        public virtual BTUser? User { get; set; }

        // Foreign keys
        public int TicketId { get; set; }
        public string? UserId { get; set; }
    }
}
