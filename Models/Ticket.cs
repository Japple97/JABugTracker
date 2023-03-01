using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace JABugTracker.Models
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string? Description { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Updated { get; set; }

        public bool Archived { get; set; }

        public bool ArchivedByProject { get; set; }

        //Nav Properties / FKs

        public int ProjectId { get; set; }
        public virtual Project? Project { get; set; }

        public int TicketTypeId { get; set; }
        public virtual TicketType? TicketType { get; set; }

        public int TicketStatusId { get; set; }
        public virtual TicketStatus? TicketStatus { get; set; }

        public int TicketPriorityId { get; set; }
        public virtual TicketPriority? TicketPriority { get; set; }

        public string? DeveloperUserId { get; set; }
        public virtual BTUser? DeveloperUser { get; set; }

        [Required(ErrorMessage = "Submitter is required")]
        public string? SubmitterUserId { get; set; }
        public virtual BTUser? SubmitterUser { get; set; }

        public virtual ICollection<TicketComment> Comments { get; set; } = new HashSet<TicketComment>();

        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new HashSet<TicketAttachment>();

        public virtual ICollection<TicketHistory> History { get; set; } = new HashSet<TicketHistory>();
    }
}