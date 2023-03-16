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
using JABugTracker.Services;

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
        private readonly IBTFileService _fileService;
        private readonly IBTTicketCommentService _commentService;

        public TicketsController(ApplicationDbContext context, UserManager<BTUser> userManager, ITicketService ticketService, IBTRoleService roleService, IBTCompanyService companyService, IBTTicketHistoryService historyService, IProjectService projectService, IBTNotificationService notificationService, IBTFileService fileService, IBTTicketCommentService commentService)
        {
            _context = context;
            _userManager = userManager;
            _ticketService = ticketService;
            _roleService = roleService;
            _companyService = companyService;
            _historyService = historyService;
            _projectService = projectService;
            _notificationService = notificationService;
            _fileService = fileService;
            _commentService = commentService;
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
        [Authorize(Roles = "Admin, Project Manager")]
        public async Task<IActionResult> AssignDeveloper(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }
            int companyId = User.Identity!.GetCompanyId();

            IEnumerable<BTUser> developers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.Developer), companyId);
            BTUser? currentDev = await _ticketService.GetCurrentDeveloperAsync(id);
            AssignDeveloperViewModel viewModel = new()
            {
                Ticket = await _ticketService.GetTicketByIdAsync(id),
                Developers = new SelectList(developers, "Id", "FullName", currentDev?.Id),
                SelectedDeveloperId = currentDev?.Id
            };
            return View(viewModel);
        }

        //POST : Tickets / AssignDeveloper---------------------------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel viewModel)
        {
            int companyId = User.Identity!.GetCompanyId();
            string? userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(viewModel.SelectedDeveloperId))
            {

                Ticket? oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(viewModel.Ticket!.Id, companyId);


                await _ticketService.AddDeveloperToTicketAsync(viewModel.Ticket?.Id, viewModel.SelectedDeveloperId);

                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(viewModel.Ticket!.Id, companyId);
                await _historyService.AddHistoryAsync(oldTicket, newTicket, userId!);

                BTUser? btUser = await _userManager.FindByIdAsync(userId!);

                //add history
                await _historyService.AddHistoryAsync(null!, newTicket!, userId!);

                //Create and add notification
                Notification? notification = new()
                {
                    TicketId = viewModel.Ticket.Id,
                    Title = "New Ticket Added",
                    Message = $"Ticket : {viewModel.Ticket.Title} was assigned by {btUser?.FullName}",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = userId,
                    RecipientId = viewModel.SelectedDeveloperId,
                    NotificationTypeId = (await _context.NotificationTypes.FirstOrDefaultAsync(n => n.Name == nameof(BTNotificationTypes.Ticket)))!.Id
                };

                await _notificationService.AddNotificationAsync(notification);
                await _notificationService.SendEmailNotificationAsync(notification, "New Developer Assigned");
                //Success
                return RedirectToAction("Details", new { id = viewModel.Ticket?.Id });
            }


            //If null, reset view and try again 
            ModelState.AddModelError("DevloperId", "No Developer Chosen. Please Select a Developer");

            IEnumerable<BTUser> developers = await _roleService.GetUsersInRoleAsync(nameof(BTRoles.Developer), companyId);
            BTUser? currentDeveloper = await _ticketService.GetCurrentDeveloperAsync(viewModel.Ticket!.Id);

            viewModel.Ticket = await _ticketService.GetTicketByIdAsync(viewModel.Ticket?.Id);
            viewModel.Developers = new SelectList(developers, "Id", "FullName", currentDeveloper.Id);
            viewModel.SelectedDeveloperId = currentDeveloper?.Id;

            return View(viewModel);
        }




        //POST: Tickets/AddTicketComment----------------------------------------------------------------COMMENT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketComment(int id, string comment)
        {
            if (string.IsNullOrEmpty(comment))
            {
                ModelState.AddModelError("Comment", "Please enter a comment");
                return RedirectToAction("Details", "Tickets", new { id = id });
            }

            TicketComment ticketComment = new()
            {
                TicketId = id,
                Comment = comment,
                UserId = _userManager.GetUserId(User),
                Created = DataUtility.GetPostGresDate(DateTime.UtcNow)
            };

            await _commentService.AddTicketCommentAsync(ticketComment);
            await _historyService.AddHistoryAsync(id, nameof(TicketComment), ticketComment.UserId);
            return RedirectToAction("Details", "Tickets", new { id = id });
        }

        //POST: Tickets/AddTicketAttachment----------------------ATTACHMENT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTicketAttachment([Bind("Id,Description,TicketId,FormFile")] TicketAttachment ticketAttachment)
        {
            //Instantiate confirmation / error message
            string message;

            //Check if a valid attachment(file) was selected
            // Remove BTUserId FK to allow valid modelstate
            ModelState.Remove("BTUserId");
            if (ModelState.IsValid && ticketAttachment.FormFile != null)
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileType = ticketAttachment.FormFile.ContentType;
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.Created = DataUtility.GetPostGresDate(DateTime.UtcNow);
                ticketAttachment.BTUserId = _userManager.GetUserId(User);

                // add attachment
                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                //add history
                await _historyService.AddHistoryAsync(ticketAttachment.TicketId, nameof(TicketAttachment), ticketAttachment.BTUserId);
                message = "Success";
            }
            else
            {
                message = "Error: Attachment Failed";
            }

            return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = message });
        }

        // GET: Tickets/Create-------------------------------------------------------------------------------CREATE
        public async Task<IActionResult> Create()
        {
            //Get company id
            int companyId = User.Identity!.GetCompanyId();

            //Get List of developers in this company to assign to this ticket.
            ViewData["DeveloperUserId"] = new SelectList(await _roleService.GetUsersInRoleAsync(nameof(BTRoles.Developer), companyId), "Id", "FullName");

            // Get List of Active Projects in this company
            ViewData["ProjectId"] = new SelectList(await _projectService.GetProjectsAsync(companyId), "Id", "Name");

            //Get List of Submitters in this Company
            ViewData["SubmitterUserId"] = new SelectList(await _roleService.GetUsersInRoleAsync(nameof(BTRoles.Submitter), companyId), "Id", "Id");

            //Get List of TicketPriorities
            ViewData["TicketPriorityId"] = new SelectList(await _ticketService.GetTicketPrioritiesAsync(), "Id", "Name");

            //Get List of TicketStatuses
            ViewData["TicketStatusId"] = new SelectList(await _ticketService.GetTicketStatusesAsync(), "Id", "Name");

            //Get List of TicketTypes
            ViewData["TicketTypeId"] = new SelectList(await _ticketService.GetTicketTypesAsync(), "Id", "Name");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Created,Updated,Archived,ArchivedByProject,ProjectId,TicketTypeId,TicketStatusId,TicketPriorityId,DeveloperUserId,SubmitterUserId")] Ticket ticket)
        {
            ModelState.Remove("SubmitterUserId");
            string? userId = _userManager.GetUserId(User);
            
            if (ModelState.IsValid)
            {
                ticket.Created = DataUtility.GetPostGresDate(DateTime.UtcNow);
                ticket.SubmitterUserId = _userManager.GetUserId(User);


                await _ticketService.AddTicketAsync(ticket);

                //Add History Record
                int companyId = User.Identity!.GetCompanyId();
                Ticket? newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id, companyId);

                BTUser? projectManager = await _projectService.GetProjectManagerAsync(ticket.ProjectId);
                BTUser? btUser = await _userManager.FindByIdAsync(userId!);

                await _historyService.AddHistoryAsync(null!, newTicket!, userId!);
                
                Notification? notification = new()
                {
                    TicketId = ticket.Id,
                    Title = "New Ticket Added",
                    Message = $"New Ticket : {ticket.Title} was created by {btUser}",
                    Created = DataUtility.GetPostGresDate(DateTime.Now),
                    SenderId = userId,
                    RecipientId = projectManager?.Id,
                    NotificationTypeId = (await _context.NotificationTypes.FirstOrDefaultAsync(n=>n.Name == nameof(BTNotificationTypes.Ticket)))!.Id
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
