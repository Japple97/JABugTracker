using JABugTracker.Models;

namespace JABugTracker.Services.Interfaces
{
	public interface ITicketService
	{
		public Task<Ticket> GetTicketByIdAsync(int? ticketId);
		public Task<Ticket> GetTicketAsNoTrackingAsync(int? ticketId, int? companyId);
		public Task<TicketAttachment> GetTicketAttachmentByIdAsync(int ticketAttachmentId);
		public Task AddTicketAttachmentAsync(TicketAttachment ticketAttachment);
		public Task<BTUser> GetCurrentDeveloperAsync(int? ticketId);
		public Task<bool> AddDeveloperToTicketAsync(int? ticketId, string userId);
		public Task<IEnumerable<Ticket>> GetIncompleteTicketAsync(int? companyId);
		public Task AddTicketAsync(Ticket ticket);
		public Task UpdateTicketAsync(Ticket ticket);
		public Task<IEnumerable<TicketPriority>> GetTicketPrioritiesAsync();
		public Task<IEnumerable<TicketStatus>> GetTicketStatusesAsync();
		public Task<IEnumerable<TicketType>> GetTicketTypesAsync();


	}
}
