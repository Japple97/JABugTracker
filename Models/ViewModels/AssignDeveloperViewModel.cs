using Microsoft.AspNetCore.Mvc.Rendering;

namespace JABugTracker.Models.ViewModels
{
	public class AssignDeveloperViewModel
	{
		public int TicketId { get; set; }
		public SelectList Developers { get; set; }
		public string SelectedDeveloperId { get; set; }

	}
}
