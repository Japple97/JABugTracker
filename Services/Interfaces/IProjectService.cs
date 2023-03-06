using JABugTracker.Models;

namespace JABugTracker.Services.Interfaces
{
    public interface IProjectService
    {
        public Task<Project> GetProjectByIdAsync(int projectId, int companyId);
    }
}
