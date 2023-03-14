using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JABugTracker.Services
{
	public class BTRoleService : IBTRoleService
	{
		private readonly UserManager<BTUser> _userManager;
		private readonly ApplicationDbContext _context;
		public BTRoleService(UserManager<BTUser> userManager, ApplicationDbContext context)
		{
			_userManager = userManager;
			_context = context;
		}

		//CTRL+M+O to minimize all methods

		public async Task<bool> AddUserToRoleAsync(BTUser user, string roleName)
		{
			try
			{
				bool result = (await _userManager.AddToRoleAsync(user, roleName)).Succeeded;
				return result;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<List<IdentityRole>> GetRolesAsync()
		{
			try
			{
				List<IdentityRole> result = new();
				result = await _context.Roles.ToListAsync();

				return result;

			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<IEnumerable<string>> GetUserRolesAsync(BTUser user)
		{
			try
			{
				IEnumerable<string> result = await _userManager.GetRolesAsync(user);
				return result;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<List<BTUser>> GetUsersInRoleAsync(string roleName, int? companyId)
		{
			try
			{
				List<BTUser> result = new();
				List<BTUser> users = new();

				users = (await _userManager.GetUsersInRoleAsync(roleName)).ToList();
				result = users.Where(u=>u.CompanyId== companyId).ToList();
				return result;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<bool> IsUserInRoleAsync(BTUser user, string roleName)
		{
			try
			{
				bool result = await _userManager.IsInRoleAsync(user, roleName);
				return result;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<bool> RemoveUserFromRoleAsync(BTUser user, string roleName)
		{
			try
			{
				bool result = (await _userManager.RemoveFromRoleAsync(user, roleName)).Succeeded;
				return result;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public async Task<bool> RemoveUserFromRolesAsync(BTUser user, IEnumerable<string> roleNames)
		{
			try
			{
				
				bool result = (await _userManager.RemoveFromRolesAsync(user, roleNames)).Succeeded;
				return result;
			}
			catch (Exception)
			{

				throw;
			}
		}
	}
}
