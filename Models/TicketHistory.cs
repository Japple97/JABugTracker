using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class TicketHistory
    {
        [Key]
        public int Id { get; set; }


        //Required?
        public string? PropertyName { get; set; }
        public string? Description { get; set; }
        public DateTime Created { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }


        //Nav Properties
        public string? UserId { get; set; }
        public BTUser? User { get; set; }
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }
    }
}
