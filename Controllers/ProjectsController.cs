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
using JABugTracker.Services;
using JABugTracker.Services.Interfaces;
using Microsoft.Build.Execution;
using Microsoft.AspNetCore.Authorization;
using Npgsql.Internal.TypeHandling;
using JABugTracker.Extensions;
using JABugTracker.Models.ViewModels;
using JABugTracker.Models.Enums;

namespace JABugTracker.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTFileService _btFileService;
        private readonly IProjectService _projectService;
        private readonly IBTRoleService _btRoleService;
        private readonly IBTCompanyService _companyService;

        public ProjectsController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTFileService btFileService, IProjectService projectService, IBTRoleService btRoleService, IBTCompanyService companyService)
        {
            _context = context;
            _userManager = userManager;
            _btFileService = btFileService;
            _projectService = projectService;
            _btRoleService = btRoleService;
            _companyService = companyService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            int companyId = User.Identity!.GetCompanyId();
           
            IEnumerable<Project> projects = await _context.Projects
                                                          .Where(p => p.Archived == false && p.CompanyId==companyId)
                                                          .Include(p=>p.Members)
                                                          .Include(p=>p.ProjectPriority)
                                                          .Include(p=>p.Tickets)
                                                          .ToListAsync();         
            return View(projects);
        }

        //GET: Projects/UnassignedProjects
        public async Task<IActionResult> UnassignedProjects()
        {
            int companyId = User.Identity!.GetCompanyId();
            
            IEnumerable<Project> unassigned = await _context.Projects.Where(p => p.Archived == false && p.CompanyId == companyId && !p.Members.Any()).Include(p => p.Members)
                                                          .Include(p => p.ProjectPriority)
                                                          .Include(p => p.Tickets).ToListAsync();
            return View(unassigned);

        }

        //GET: Projects/ArchivedProjects
        public async Task<IActionResult> ArchivedProjects()
        {
            int companyId = User.Identity!.GetCompanyId();
            IEnumerable<Project> archived = await _context.Projects.Where(p => p.Archived == true && p.CompanyId == companyId)
                                                              .Include(p => p.Members)
                                                              .Include(p => p.ProjectPriority)
                                                              .Include(p => p.Tickets).ToListAsync();
            return View(archived);
        }

        // GET: Projects/MyProjects---------------------------------------------------------------------------------------
        public async Task<IActionResult> MyProjects()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            int companyId = User.Identity!.GetCompanyId();
            var projects = await _context.Projects
                .Where(p => p.CompanyId == companyId && p.Members.Contains(currentUser!) && p.Archived==false)
                .Include(p=>p.Members)
                .Include(p=>p.Tickets)
                .ToListAsync();
            return View(projects);
        }

        // GET: Projects/Details/5-----------------------------------------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            
            int companyId = User.Identity!.GetCompanyId();

            Project? project = await _projectService.GetProjectByIdAsync(id, companyId);
            
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }


		// GET: Projects/AssignPM------------------------------------------------------------------------------------------
		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AssignPM(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //Get companyId
            int companyId = User.Identity!.GetCompanyId();

            IEnumerable<BTUser> projectManagers = await _btRoleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(id);
            AssignPMViewModel viewModel = new()
            {
                Project = await _projectService.GetProjectByIdAsync(id, companyId),
                PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id),
                PMId = currentPM?.Id,
            };
            return View(viewModel);

        }

        // POST: Projects/AssignPM----------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AssignPM(AssignPMViewModel viewModel)
        {
            if (!string.IsNullOrEmpty(viewModel.PMId))
            {
                await _projectService.AddProjectManagerAsync(viewModel.PMId, viewModel.Project?.Id);

                return RedirectToAction("Details", new { id = viewModel.Project?.Id });

            }

            ModelState.AddModelError("PMId", "No Project Manager chosen. Please select a PM.");
            int companyId = User.Identity!.GetCompanyId();

            IEnumerable<BTUser> projectManagers = await _btRoleService.GetUsersInRoleAsync(nameof(BTRoles.ProjectManager), companyId);
            BTUser? currentPM = await _projectService.GetProjectManagerAsync(viewModel.Project?.Id);
            viewModel.Project = await _projectService.GetProjectByIdAsync(viewModel.Project?.Id, companyId);
            viewModel.PMList = new SelectList(projectManagers, "Id", "FullName", currentPM?.Id);
            viewModel.PMId = currentPM?.Id;

            return View(viewModel);

        }


        //GET: Projects/AssignProjectMembers----------------------------------------------------------------------------
        [HttpGet]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignProjectMembers(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            int companyId = User.Identity!.GetCompanyId();

            Project project = await _projectService.GetProjectByIdAsync(id, companyId);

            List<BTUser> submitters = await _btRoleService.GetUsersInRoleAsync(nameof(BTRoles.Submitter), companyId);
            List<BTUser> developers = await _btRoleService.GetUsersInRoleAsync(nameof(BTRoles.Developer), companyId);
            //Merge the 2 above lists
            List<BTUser> usersList = submitters.Concat(developers).ToList();

            List<string> currentMembers = project.Members.Select(m => m.Id).ToList();



            ProjectMembersViewModel viewModel = new()
            {
                Project = project,
                UsersList = new MultiSelectList(usersList, "Id", "FullName", currentMembers)
            };

            return View(viewModel);
        }

        //POST: Projects/AssignProjectMembers-------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> AssignProjectMembers(ProjectMembersViewModel viewModel)
        {
            int companyId = User.Identity!.GetCompanyId();

            if (viewModel.SelectedMembers != null)
            {
                //Remove current members
                await _projectService.RemoveMembersFromProjectAsync(viewModel.Project!.Id, companyId);

                //Add newly selected members
                await _projectService.AddMembersToProjectAsync(viewModel.SelectedMembers, viewModel.Project!.Id, companyId);

                return RedirectToAction(nameof(Details), new { viewModel.Project!.Id });

            }

            ModelState.AddModelError("SelectedMembers", "No members were selected. Please select members to add to the project.");
            //if NOT, reset the form
            viewModel.Project = await _projectService.GetProjectByIdAsync(viewModel.Project!.Id, companyId);
            List<string> currentMembers = viewModel.Project.Members.Select(m => m.Id).ToList();

            List<BTUser> submitters = await _btRoleService.GetUsersInRoleAsync(nameof(BTRoles.Submitter), companyId);
            List<BTUser> developers = await _btRoleService.GetUsersInRoleAsync(nameof(BTRoles.Developer), companyId);
            //Merge the 2 above lists
            List<BTUser> usersList = submitters.Concat(developers).ToList();
            viewModel.UsersList = new MultiSelectList(usersList, "Id", "FullName", currentMembers);

            return View(viewModel);


        }

        // GET: Projects/Create----------------------------------------------------------------------------------------------
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            //Get companyId
            int companyId = User.Identity!.GetCompanyId();

            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name");
            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name");
            return View();
        }

        // POST: Projects/Create---------------------------------------
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Created,StartDate,EndDate,ImageFormFile,Archived,CompanyId,ProjectPriorityId")] Project project)
        {
            //Get companyId
            int companyId = User.Identity!.GetCompanyId();
            if (ModelState.IsValid)
            {
                //Assign Company
                project.CompanyId= companyId;
                //Format Date
                project.Created = DataUtility.GetPostGresDate(DateTime.UtcNow);
                project.StartDate = DataUtility.GetPostGresDate(project.StartDate);
                project.EndDate = DataUtility.GetPostGresDate(project.EndDate);

                //Image
                if (project.ImageFormFile != null)
                {
                    project.ImageFileData = await _btFileService.ConvertFileToByteArrayAsync(project.ImageFormFile);
                    project.ImageFileType = project.ImageFormFile.ContentType;
                }

                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", companyId);
            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // GET: Projects/Edit/5----------------------------------------------------------------------------------------------------------------------------------
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            if (project.ImageFormFile != null)
            {
                project.ImageFileData = await _btFileService.ConvertFileToByteArrayAsync(project.ImageFormFile);
                project.ImageFileType = project.ImageFormFile.ContentType;
            }

            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", project.CompanyId);
            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // POST: Projects/Edit/5---------------------------------------------------------------------
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, ProjectManager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Created,StartDate,EndDate,ImageFormFile,Archived,CompanyId,ProjectPriorityId")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }
            //Get companyId
            int companyId = User.Identity!.GetCompanyId();
            if (ModelState.IsValid)
            {
                try
                {
                    //Assign Company
                    project.CompanyId = companyId;
                    // Reformat Dates
                    project.Created = DateTime.SpecifyKind(project.Created, DateTimeKind.Utc);
                    project.StartDate = DataUtility.GetPostGresDate(project.StartDate);
                    project.EndDate = DataUtility.GetPostGresDate(project.EndDate);
                    //Image 
                    if(project.ImageFormFile != null)
                    {
                        project.ImageFileData = await _btFileService.ConvertFileToByteArrayAsync(project.ImageFormFile);
                        project.ImageFileType = project.ImageFormFile.ContentType;
                    }



                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
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
            ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Name", project.CompanyId);
            ViewData["ProjectPriorityId"] = new SelectList(_context.ProjectPriorities, "Id", "Name", project.ProjectPriorityId);
            return View(project);
        }

        // GET: Projects/Delete/5--------------------------------------------------------------------------------------------------------------
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Projects == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ProjectPriority)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // POST: Projects/Delete/5---------------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Projects == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Projects'  is null.");
            }
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                project.Archived = true;
                _context.Projects.Remove(project);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // --------------------------------------
        private bool ProjectExists(int id)
        {
          return (_context.Projects?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
