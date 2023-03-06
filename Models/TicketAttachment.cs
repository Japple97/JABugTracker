using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JABugTracker.Models
{
    public class TicketAttachment
    {
        [Key]
        public int Id { get; set; }

        public string? Description { get; set; }

        public DateTime Created { get; set; }


        [NotMapped]
        public IFormFile? FormFile { get; set; }

        public byte[]? FileData { get; set; }

        public string? FileType { get; set; }

        public string? FileName { get; set; }

        // Navigation properties
        public virtual Ticket? Ticket { get; set; }

        public virtual BTUser? BTUser { get; set; }

        public int TicketId { get; set; }

        public string? BTUserId { get; set; }
    }
}
