using JABugTracker.Models;

namespace JABugTracker.Services.Interfaces
{
    public interface IProjectService
    {
        //public Task AddProjectAsync(Project project);
        //public Task ArchiveProjectAsync(Project project);
        //public Task<List<Project>> GetAllProjectsByCompanyIdAsync(int companyId);
        //public Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int companyId);
        //public Task AddMembersToProjectAsync(IEnumerable<string> userIds, int projectId);



        public Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId);
        public Task<Project> GetProjectByIdAsync(int? projectId, int? companyId);
        public Task<bool> AddProjectManagerAsync(string? userId, int? projectId);
        public Task<BTUser> GetProjectManagerAsync(int? projectId);

        public Task RemoveProjectManagerAsync(int? projectId);
        public Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId);
    }
}
