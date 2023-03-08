using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JABugTracker.Services
{
    public class BTCompanyService : IBTCompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        public BTCompanyService(ApplicationDbContext context, UserManager<BTUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public async Task<Company> GetCompanyInfoAsync(int? companyId)
        {
            try
            {
                Company? company = new();
                if (companyId != null)
                {
                    company = await _context.Companies.Include(c => c.Members).Include(c => c.Invites).Include(c => c.Projects).FirstOrDefaultAsync(c => c.Id == companyId);
                }
              
                return company!;

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<BTUser>> GetMembersAsync(int? companyId)
        {
            try
            {
               
                List<BTUser>? members = new();
                members = await _context.Users.Where(u=>u.CompanyId == companyId).ToListAsync();
                return members;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
