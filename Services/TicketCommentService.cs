using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JABugTracker.Services
{
    public class TicketCommentService : IBTTicketCommentService
    {
        private readonly ApplicationDbContext _context;
        public TicketCommentService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task AddTicketCommentAsync(TicketComment ticketComment)
        {
            try
            {
                _context.TicketComments.Add(ticketComment);
                await _context.SaveChangesAsync();

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<IEnumerable<TicketComment>> GetTicketCommentsAsync(int? id)
        {
            try
            {
                return await _context.TicketComments.Where(t => t.TicketId == id).ToListAsync();
            }

            catch (Exception)
            {

                throw;
            }
        }
    }
}
