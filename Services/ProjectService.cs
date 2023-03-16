using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Models.Enums;
using JABugTracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.ComponentModel.Design;

namespace JABugTracker.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
		private readonly IBTRoleService _roleService;
        public ProjectService(ApplicationDbContext context, IBTRoleService roleService)
        {
            _context = context;
			_roleService = roleService;
        }

        public async Task AddMembersToProjectAsync(IEnumerable<string> userIds, int? projectId, int? companyId)
        {
			try
			{
				Project? project = await GetProjectByIdAsync(projectId, companyId);

				foreach(string userId in userIds)
				{
					BTUser? btUser = await _context.Users.FindAsync(userId);

					if (project != null && btUser != null)
					{
						bool IsOnProject = project.Members.Any(m => m.Id == userId);

						if (!IsOnProject) 
						{
							project.Members.Add(btUser);
						}
						else
						{
							continue;
						}
					}
				}
				await _context.SaveChangesAsync();
			}
			catch (Exception)
			{

				throw;
			}
        }

        public async Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId)
		{
			try
			{
				Project? project = await GetProjectByIdAsync(projectId, member!.CompanyId);

				bool IsOnProject = project.Members.Any(m => m.Id == member.Id);

				if(!IsOnProject)
				{
					project.Members.Add(member);
					await _context.SaveChangesAsync();
					return true;
				} 

				return false;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<bool> AddProjectManagerAsync(string? userId, int? projectId)
		{
			try
			{
				BTUser? currentPM = await GetProjectManagerAsync(projectId);
				BTUser? selectedPM = await _context.Users.FindAsync(userId);

				//Remove current PM
				if(currentPM != null)
				{
					await RemoveProjectManagerAsync(projectId);
				}
				// Add new selected PM
				try
				{
					await AddMemberToProjectAsync(selectedPM!, projectId);
					return true;
				}
				catch (Exception)
				{

					throw;
				}

			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<Project> GetProjectByIdAsync(int? projectId, int? companyId)
        {
            try
            {

                Project? project = await _context.Projects
										     .Where(p => p.CompanyId == companyId)
                                             .Include(p => p.Company)
                                             .Include(p => p.ProjectPriority)
                                             .Include(p => p.Members)
                                             .Include(p => p.Tickets)
                                                   .ThenInclude(t => t.DeveloperUser)
                                               .Include(p => p.Tickets)
                                                   .ThenInclude(t => t.SubmitterUser)
                                             .FirstOrDefaultAsync(m => m.Id == projectId);
                return project!;
            }
			catch (Exception)
			{

				throw;
			}
        }

		public async Task<BTUser> GetProjectManagerAsync(int? projectId)
		{
			try
			{
				Project? project = await _context.Projects.Include(p=>p.Members).FirstOrDefaultAsync(p=>p.Id == projectId);
				foreach (BTUser member in project!.Members)
				{
					if (await _roleService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
					{
						return member;
					}

				}
				return null!;
			}
			catch (Exception)
			{

				throw;
			}
		}

        public async Task<IEnumerable<Project>> GetProjectsAsync(int? companyId)
        {
			try
			{
				IEnumerable<Project> projects = await _context.Projects.Where(p=>p.Archived == false && p.CompanyId == companyId).Include(p=>p.Members).Include(p=>p.Tickets).Include(p=>p.ProjectPriority).ToListAsync();
				return projects;

			}
			catch (Exception)
			{

				throw;
			}
        }

        public async Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId)
		{
			try
			{
				Project? project = await GetProjectByIdAsync(projectId, member!.CompanyId);
				bool IsOnProject = project.Members.Any(m=>m.Id == member.Id);
				if(IsOnProject)
				{
					project.Members.Remove(member);
					await _context.SaveChangesAsync();
					return true;
				}
				return false;
			}
			catch (Exception)
			{

				throw;
			}
		}

        public async Task RemoveMembersFromProjectAsync(int? projectId, int? companyId)
        {
			try
			{
				Project? project = await GetProjectByIdAsync(projectId, companyId);

				foreach (BTUser member in project.Members)
				{
					if(!await _roleService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
					{
						project.Members.Remove(member);
					}
				}
				await _context.SaveChangesAsync();
			}
			catch (Exception)
			{

				throw;
			}
        }

        public async Task RemoveProjectManagerAsync(int? projectId)
		{
			try
			{
				Project? project = await _context.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId);
				foreach (BTUser member in project!.Members)
				{
					if (await _roleService.IsUserInRoleAsync(member, nameof(BTRoles.ProjectManager)))
					{
						await RemoveMemberFromProjectAsync(member, projectId);
					}
				}				
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}
