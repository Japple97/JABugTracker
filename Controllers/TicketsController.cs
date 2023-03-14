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
using JABugTracker.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Org.BouncyCastle.Bcpg;
using Microsoft.AspNetCore.Authentication;
using JABugTracker.Models.Enums;

namespace JABugTracker.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly ITicketService _ticketService;
        private readonly IBTRoleService _roleService;
        private readonly IBTCompanyService _companyService;
        private readonly IBTTicketHistoryService _historyService;
        private readonly IProjectService _projectService;
        private readonly IBTNotificationService _notificationService;

        public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, ITicketService ticketService, IBTRoleService roleService, IBTCompanyService companyService, IBTTicketHistoryService historyService, IProjectService projectService, IBTNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _ticketService = ticketService;
            _roleService = roleService;
            _companyService = companyService;
            _historyService = historyService;
            _projectService = projectService;
            _notificationService = notificationService;
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


        // GET: Tickets / AssignDeveloper--------------------------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> AssignDeveloper(int id)
        {
            Ticket? ticket = await _ticketService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            int companyId = User.Identity!.GetCompanyId();

            // Get a list of developers
            List<BTUser> developers = await _roleService.GetUsersInRoleAsync("Developer", companyId);
            var devList = new SelectList(developers, "Id", "FullName");
            AssignDeveloperViewModel viewModel = new()
            {
                TicketId = ticket.Id,
                Developers = devList
            };
            return View(viewModel);
        }

        //POST : Tickets / AssignDeveloper---------------------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            int companyId = User.Identity!.GetCompanyId();
           
            Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(viewModel.TicketId, companyId);

            var ticket = await _context.Tickets.FindAsync(viewModel.TicketId);

            if (ticket == null)
            {
                return NotFound();
            }

            ticket.DeveloperUserId = viewModel.SelectedDeveloperId;
            _context.Update(ticket);
            await _context.SaveChangesAsync();


            //TODO: add history
            string? userId = _userManager.GetUserId(User);

            BTUser? btUser = await _userManager.GetUserAsync(User);
            Notification? notification = new()
            {
                TicketId = ticket!.Id,
                Title = "Developer Assigned",
                Message = $"New Ticket: {ticket.Title} was created by {btUser?.FullName}",
                Created = DataUtility.GetPostGresDate(DateTime.Now),
                SenderId = userId,
                RecipientId =ticket.DeveloperUserId,
                NotificationTypeId = (await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == nameof(BTNotificationTypes.Ticket)))!.Id
            };


                await _notificationService.AddNotificationAsync(notification);
                await _notificationService.SendEmailNotificationAsync(notification, "New Ticket Added");





            return RedirectToAction(nameof(Index));
        }




        //POST: Tickets/AddTicketComment----------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddTicketComment([Bind("Id, TicketId, Comment")] TicketComment ticketComment)
        //{
        //    try
        //    {
        //        if (ticketId != null && !string.IsNullOrEmpty(comment))
        //        {
        //            TicketComment commentNew = new TicketComment
        //            {
        //                TicketId = ticketId.Value,
        //                Comment = comment,
        //                UserId = _userManager.GetUserId(User),
        //                Created = DataUtility.GetPostGresDate(DateTime.UtcNow)
        //            };
        //            _context.TicketComments.Add(commentNew);
        //            await _context.SaveChangesAsync();
        //        }
        //        //ADD history
        //        await _historyService.AddHistoryAsync()
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }

        //    return RedirectToAction("Details", "Tickets", new { id = ticketId });

        //}

        //TODO: TICKET ATTACHMENT POST METHOD*----------------------

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
            BTUser? btUser = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid)
            {

                string? userId = _userManager.GetUserId(User);
                // Format Date
                ticket.Created = DataUtility.GetPostGresDate(DateTime.UtcNow);
                ticket.Updated = DataUtility.GetPostGresDate(DateTime.UtcNow);
                ticket.SubmitterUserId = userId;
                // ticket.TicketStatusId = (await _context.TicketStatuses.FirstOrDefaultAsync(s=>s.Name == nameof))





                _context.Add(ticket);
                await _context.SaveChangesAsync();

                // Add Ticket History
                int companyId = User.Identity!.GetCompanyId();
                //change to GetTicketAsNoTrackingAsync in service
                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, companyId);

                await _historyService.AddHistoryAsync(null, newTicket, userId);

                //TODO: Notification
                BTUser? projectManager = await _projectService.GetProjectManagerAsync(ticket.ProjectId);

                Notification? notification = new()
                {
                    TicketId = ticket.Id,
                    Title = "New Ticket Added",
                    Message = $"New Ticket: {ticket.Title} was created by {btUser?.FullName}",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = userId,
                    RecipientId = projectManager?.Id,
                    NotificationTypeId = (await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == nameof(BTNotificationTypes.Ticket)))!.Id
                };

                if (projectManager != null)
                {
                    await _notificationService.AddNotificationAsync(notification);
                    await _notificationService.SendEmailNotificationAsync(notification, "New Ticket Added");
                }
                else
                {
                    await _notificationService.AdminNotificationAsync(notification, companyId);
                    await _notificationService.SendAdminEmailNotificationAsync(notification, "New Project Ticket Added", companyId);
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
                int companyId = User.Identity!.GetCompanyId();
                string? userId = _userManager.GetUserId(User);
                Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, companyId);
                try
                {
                    // Reformat Date
                    ticket.Created = DataUtility.GetPostGresDate(ticket.Created);
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

                //change to GetTicketAsNoTrackingAsync in service
                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, companyId);

                await _historyService.AddHistoryAsync(oldTicket, newTicket, userId);

                //TODO: Notification
                BTUser? btUser = await _userManager.GetUserAsync(User);
                BTUser? projectManager = await _projectService.GetProjectManagerAsync(ticket.ProjectId);

                Notification? notification = new()
                {
                    TicketId = ticket.Id,
                    Title = "New Ticket Added",
                    Message = $"New Ticket: {ticket.Title} was created by {btUser?.FullName}",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = userId,
                    RecipientId = projectManager?.Id,
                    NotificationTypeId = (await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == nameof(BTNotificationTypes.Ticket)))!.Id
                };

                if (projectManager != null)
                {
                    await _notificationService.AddNotificationAsync(notification);
                    await _notificationService.SendEmailNotificationAsync(notification, "New Ticket Added");
                }
                else
                {
                    await _notificationService.AdminNotificationAsync(notification, companyId);
                    await _notificationService.SendAdminEmailNotificationAsync(notification, "New Project Ticket Added", companyId);
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
