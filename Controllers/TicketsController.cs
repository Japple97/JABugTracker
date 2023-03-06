using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JABugTracker.Data;
using JABugTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis;
using JABugTracker.Extensions;
using JABugTracker.Services.Interfaces;

namespace JABugTracker.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly ITicketService _ticketService;

        public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, ITicketService ticketService)
        {
            _context = context;
            _userManager = userManager;
            _ticketService = ticketService;
        }

        // GET: Tickets---------------------------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            IEnumerable<Ticket> tickets = await _context.Tickets.Where(t => t.Archived == false).ToListAsync();


            return View(tickets);
        }

        //GET: Tickets/MyTickets------------------------------------------------------------------------
        public async Task<IActionResult> MyTickets(int? projectId)
        {

            if (projectId == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            int companyId = User.Identity!.GetCompanyId();

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

            if (project == null || !project.Members.Contains(currentUser!))
            {
                return NotFound();
            }

            var role = await _userManager.GetRolesAsync(currentUser);
            List<Ticket> tickets = new();

            if (role.Contains("Admin") || role.Contains("ProjectManager"))
            {
                tickets = await _context.Tickets
                    .Where(t => t.ProjectId == projectId && t.Archived == false)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.SubmitterUser)
                    .ToListAsync();
            }
            else if (role.Contains("Developer"))
            {
                tickets = await _context.Tickets
                    .Where(t => t.ProjectId == projectId && t.DeveloperUserId == currentUser.Id && t.Archived == false)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.SubmitterUser)
                    .ToListAsync();
            }
            else if (role.Contains("Submitter"))
            {
                tickets = await _context.Tickets
                    .Where(t => t.ProjectId == projectId && t.SubmitterUserId == currentUser.Id && t.Archived == false)
                    .Include(t => t.DeveloperUser)
                    .Include(t => t.SubmitterUser)
                    .ToListAsync();
            }

            return View(tickets);
        }


        // GET: Tickets/Details/5-----------------------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        //POST: Tickets/AddTicketComment----------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketComment(int? ticketId, string? comment)
        {
            try
            {
                if (ticketId != null && !string.IsNullOrEmpty(comment))
                {
                    TicketComment commentNew = new TicketComment
                    {
                        TicketId = ticketId.Value,
                        Comment = comment,
                        UserId = _userManager.GetUserId(User),
                        Created = DataUtility.GetPostGresDate(DateTime.UtcNow)
                    };
                    _context.TicketComments.Add(commentNew);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
            
            return RedirectToAction("Details", "Tickets", new { id = ticketId});

        }

        // GET: Tickets/Create-------------------------------------------------------------------------------
        public IActionResult Create()
        {
            ViewData["DeveloperUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id");
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            ViewData["SubmitterUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id");
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name");
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Name");
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Name");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                // Format Date
                ticket.Created = DataUtility.GetPostGresDate(DateTime.UtcNow);
                ticket.Updated = DataUtility.GetPostGresDate(DateTime.UtcNow);


                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeveloperUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            ViewData["DeveloperUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Reformat Date
                    ticket.Created = DataUtility.GetPostGresDate(DateTime.UtcNow);
                    ticket.Updated = DataUtility.GetPostGresDate(DateTime.UtcNow);

                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DeveloperUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id", ticket.DeveloperUserId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", ticket.ProjectId);
            ViewData["SubmitterUserId"] = new SelectList(_context.Set<BTUser>(), "Id", "Id", ticket.SubmitterUserId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewData["TicketTypeId"] = new SelectList(_context.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.DeveloperUser)
                .Include(t => t.Project)
                .Include(t => t.SubmitterUser)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.TicketType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tickets == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Tickets'  is null.");
            }
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
          return (_context.Tickets?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
