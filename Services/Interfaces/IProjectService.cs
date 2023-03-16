using JABugTracker.Models;

namespace JABugTracker.Services.Interfaces
{
    public interface IProjectService
    {
        //public Task AddProjectAsync(Project project);
        //public Task ArchiveProjectAsync(Project project);




        public Task AddMembersToProjectAsync(IEnumerable<string> userIds, int? projectId, int? companyId);
        public Task RemoveMembersFromProjectAsync(int? projectId, int? companyId);


        public Task<bool> AddMemberToProjectAsync(BTUser? member, int? projectId);
        public Task<Project> GetProjectByIdAsync(int? projectId, int? companyId);
        public Task<bool> AddProjectManagerAsync(string? userId, int? projectId);
        public Task<BTUser> GetProjectManagerAsync(int? projectId);

        public Task RemoveProjectManagerAsync(int? projectId);
        public Task<bool> RemoveMemberFromProjectAsync(BTUser? member, int? projectId);

        //Gets projects that are not archived
        public Task<IEnumerable<Project>> GetProjectsAsync(int? companyId);

        //Gets archived projects
        //public Task<List<Project>> GetArchivedProjectsByCompanyIdAsync(int companyId);
    }
}
