using Microsoft.AspNetCore.Mvc.Rendering;

namespace JABugTracker.Models.ViewModels
{
	public class AssignDeveloperViewModel
	{
		public Ticket? Ticket { get; set; }
		public SelectList Developers { get; set; }
		public string SelectedDeveloperId { get; set; }

	}
}
