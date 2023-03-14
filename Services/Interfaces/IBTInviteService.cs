using JABugTracker.Models;
using Microsoft.Build.Framework;

namespace JABugTracker.Services.Interfaces
{
    public interface IBTInviteService
    {
        public Task<bool> AcceptInviteAsync(Guid? token, string? userId);
        public Task AddNewInviteAsync(Invite? invite);
        public Task<bool> AnyInviteAsync(Guid? token, string? email, int? companyId);
        public Task<Invite?> GetInviteAsync(int? inviteId, int? companyId);
        public Task<Invite?> GetInviteAsync(Guid? token, string? email, int? companyId);
        public Task<bool> ValidateInviteCodeAsync(Guid? token);
    }
}
