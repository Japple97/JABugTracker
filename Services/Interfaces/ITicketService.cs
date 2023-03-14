using JABugTracker.Models;

namespace JABugTracker.Services.Interfaces
{
	public interface ITicketService
	{
		public Task<Ticket> GetTicketByIdAsync(int ticketId);
		public Task<Ticket> GetTicketAsNoTrackingAsync(int? ticketId, int? companyId);

	}
}
