using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JABugTracker.Data;
using JABugTracker.Models;
using Microsoft.AspNetCore.Authorization;
using JABugTracker.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using JABugTracker.Services.Interfaces;
using JABugTracker.Extensions;
using JABugTracker.Models.Enums;

namespace JABugTracker.Controllers
{
    [Authorize]
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BTUser> _userManager;
        private readonly IBTRoleService _roleService;
        private readonly IBTCompanyService _companyService;

        public CompaniesController(ApplicationDbContext context, UserManager<BTUser> userManager, IBTRoleService roleService, IBTCompanyService companyService)
        {
            _context = context;
            _userManager = userManager;
            _roleService = roleService;
            _companyService = companyService;
        }

        // GET: Companies
        public async Task<IActionResult> Index()
        {
              return _context.Companies != null ? 
                          View(await _context.Companies.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
        }

        //GET: Companies/ManageUserRoles--------------------------------------------------------------------------------------------------
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUserRoles()
        {
            //1- Add an instance of the viewModel as a List (model)

            List<ManageUserRolesViewModel> viewModels = new();


            //2- Get companyId
            int companyId = User.Identity!.GetCompanyId();

            //3-Get all company users
            List<BTUser> members = await _companyService.GetMembersAsync(companyId);
            List<IdentityRole> roles = (await _roleService.GetRolesAsync()).ToList();

            foreach (BTUser member in members)
            {
                IEnumerable<string> currentRoles = await _roleService.GetUserRolesAsync(member);

                ManageUserRolesViewModel viewModel = new()
                {
                    BTUser = member,
                    Roles = new MultiSelectList(roles, "Name", "Name", currentRoles)
                };
                viewModels.Add(viewModel);
            }
            return View(viewModels);
        }

        //POST: Companies/ManageUserRoles-------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel viewModel)
        {
            //1- Get the company Id
            int? companyId = User.Identity!.GetCompanyId();
            //2 - Instantiate the BTUser
            BTUser? user = await _userManager.FindByIdAsync(viewModel.BTUser!.Id);


            //3 - Get the roles for the User
            List<string> currentRoles = (await _roleService.GetUserRolesAsync(user!)).ToList();

            //4 - Get the selected Role(s) for the user submitted from the form
            List<string> selectedRoles = viewModel.SelectedRoles!;
            //5 - Remove the current role(s) and add the new role
            await _roleService.RemoveUserFromRolesAsync(user!, currentRoles);
            foreach(string role in selectedRoles)
            {
                await _roleService.AddUserToRoleAsync(user!, role);
            }
            //6 - Navigate
            return RedirectToAction("Index", "Home");

        }

        // GET: Companies/Details/5---------------------------------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Companies == null)
            {
                return NotFound();
            }

            var company = await _companyService.GetCompanyInfoAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // GET: Companies/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,ImageFileData,ImageFileType")] Company company)
        {
            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // GET: Companies/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Companies == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // POST: Companies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,ImageFileData,ImageFileType")] Company company)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(company);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyExists(company.Id))
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
            return View(company);
        }

        // GET: Companies/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Companies == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Companies == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
            }
            var company = await _context.Companies.FindAsync(id);
            if (company != null)
            {
                _context.Companies.Remove(company);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CompanyExists(int id)
        {
          return (_context.Companies?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
