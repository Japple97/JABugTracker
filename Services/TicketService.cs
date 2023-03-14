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
        //TODO
        public async Task<Ticket> GetTicketAsNoTrackingAsync(int? ticketId, int? companyId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets
                                                 .Include(t => t.Project)
                                                    .ThenInclude(p => p!.Company)
                                                .Include(t => t.Attachments)
                                                .Include(t => t.Comments)
                                                .Include(t => t.DeveloperUser)
                                                .Include(t => t.History)
                                                .Include(t => t.SubmitterUser)
                                                .Include(t => t.TicketPriority)
                                                .Include(t => t.TicketStatus)
                                                .Include(t => t.TicketType)
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(t => t.Id == ticketId && t.Project!.CompanyId == companyId && t.Archived == false);

                return ticket!;

            }
            catch (Exception)
            {

                throw;
            }
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
