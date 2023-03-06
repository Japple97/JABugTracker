using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;

namespace JABugTracker.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<Project> GetProjectByIdAsync(int projectId, int companyId)
        {
            try
            {

                Project? project = await _context.Projects.Where(p => p.CompanyId == companyId)
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
    }
}
