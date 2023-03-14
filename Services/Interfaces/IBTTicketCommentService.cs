using JABugTracker.Models;

namespace JABugTracker.Services.Interfaces
{
    public interface IBTTicketCommentService
    {
        public Task<IEnumerable<TicketComment>> GetTicketCommentsAsync(int? id);
        public Task AddTicketCommentAsync(TicketComment ticketComment);

    }
}
