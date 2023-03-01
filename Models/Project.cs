using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JABugTracker.Models
{
    public class Project
    {
        public int Id { get; set; }



        [Required(ErrorMessage = "Project name is required")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Project description is required")]
        public string? Description { get; set; }

        public DateTime Created { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        //Image Properties
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public byte[]? ImageData { get; set; }

        public string? ImageType { get; set; }
        //

        public bool Archived { get; set; }

        // Navigation properties
        public virtual Company? Company { get; set; }
        public virtual ProjectPriority? ProjectPriority { get; set; }
        public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();

        //FK
        [Display(Name = "Company")]
        public int CompanyId { get; set; }
        //
        //FK
        [Display(Name = "Priority")]
        public int ProjectPriorityId { get; set; }
        //

    }
}