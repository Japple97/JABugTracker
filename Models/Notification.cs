using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "Title is required")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Message is required")]
        public string? Message { get; set; }

        public DateTime Created { get; set; }

        public bool HasBeenViewed { get; set; }

        // Navigation properties
        public virtual NotificationType? NotificationType { get; set; }
        public int NotificationTypeId { get; set; }

        public string? SenderId { get; set; }
        public virtual BTUser? Sender { get; set; }

        public string? RecipientId { get; set; }
        public virtual BTUser? Recipient { get; set; }


        public int? ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public int? TicketId { get; set; }
        public virtual Ticket? Ticket { get; set; }
    }
}
