using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JABugTracker.Services
{
	public class TicketService : ITicketService
	{
		private readonly ApplicationDbContext _context;
		public TicketService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<Ticket> GetTicketByIdAsync(int ticketId)
		{
			try
			{
				Ticket? ticket = await _context.Tickets
											  .Include(t => t.DeveloperUser)
											  .Include(t => t.Project)
											  .Include(t => t.SubmitterUser)
											  .Include(t => t.TicketPriority)
											  .Include(t => t.TicketStatus)
											  .Include(t => t.TicketType)
											  .Include(t => t.Comments)
											  .Include(t => t.Attachments)
											  .Include(t => t.History)
											  .FirstOrDefaultAsync(m => m.Id == ticketId);
				return ticket!;
			}
			catch (Exception)
			{

				throw;
			}
		}
	}

}
