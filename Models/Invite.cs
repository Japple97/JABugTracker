using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class Invite
    {
        [Key]
        public int Id { get; set; }

        public DateTime InviteDate { get; set; }

        public DateTime? JoinDate { get; set; }

        public Guid CompanyToken { get; set; }



        [Required(ErrorMessage = "Invitee email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Invitee Email")]
        public string? InviteeEmail { get; set; }

        [Required(ErrorMessage = "Invitee first name is required")]
        [Display(Name = "Invitee First Name")]
        public string? InviteeFirstName { get; set; }

        [Required(ErrorMessage = "Invitee last name is required")]
        [Display(Name = "Invitee Last Name")]
        public string? InviteeLastName { get; set; }

        public string? Message { get; set; }

        public bool IsValid { get; set; }

        //Nav Properties
        public int CompanyId { get; set; }

        public virtual Company? Company { get; set; }

        public int ProjectId { get; set; }

        public virtual Project? Project { get; set; }

        [Required]
        public string? InvitorId { get; set; }

        public virtual BTUser? Invitor { get; set; }


        public string? InviteeId { get; set; }

        public virtual BTUser? Invitee { get; set; }
    }
}