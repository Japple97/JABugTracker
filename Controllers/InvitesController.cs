using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using JABugTracker.Data;
using JABugTracker.Models;
using JABugTracker.Services.Interfaces;
using JABugTracker.Extensions;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;

namespace JABugTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InvitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBTInviteService _inviteService;
        private readonly IProjectService _projectService;
        private readonly IBTCompanyService _companyService;
        private readonly IEmailSender _emailService;
        private readonly UserManager<BTUser> _userManager;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _configuration;

        public InvitesController(ApplicationDbContext context, IBTInviteService inviteService, IProjectService projectService, IBTCompanyService companyService, IEmailSender emailService, UserManager<BTUser> userManager, IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
        {
            _context = context;
            _inviteService = inviteService;
            _projectService = projectService;
            _companyService = companyService;
            _emailService = emailService;
            _userManager = userManager;
            _configuration = configuration;
            _protector = dataProtectionProvider.CreateProtector(configuration.GetValue<string>("ProtectKey")! ?? Environment.GetEnvironmentVariable("ProtectKey")!);

        }

        // GET: Invites
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invites.Include(i => i.Company).Include(i => i.Invitee).Include(i => i.Invitor).Include(i => i.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Invites == null)
            {
                return NotFound();
            }

            var invite = await _context.Invites
                .Include(i => i.Company)
                .Include(i => i.Invitee)
                .Include(i => i.Invitor)
                .Include(i => i.Project)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invite == null)
            {
                return NotFound();
            }

            return View(invite);
        }

        // GET: Invites/Create
        public IActionResult Create()
        {
            int companyId = User.Identity!.GetCompanyId();

            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.CompanyId == companyId), "Id", "Description");
            return View();
        }

        // POST: Invites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InviteeEmail,InviteeFirstName,InviteeLastName,Message,ProjectId")] Invite invite)
        {

            int companyId = User.Identity!.GetCompanyId();
            ModelState.Remove("InvitorId");
            if (ModelState.IsValid)
            {
                try
                {
                    Guid guid = Guid.NewGuid();

                    string? token = _protector.Protect(guid.ToString());
                    string? email = _protector.Protect(invite.InviteeEmail!);
                    string? company = _protector.Protect(companyId.ToString());

                    string? callbackUrl = Url.Action("ProcessInvite", "Invites", new { token, email, company }, protocol: Request.Scheme);

                    string body = $@"{invite.Message} <br />
                        Please join my company. <br />
                        Click the following link to join our team. <br />
                        <a href=""{callbackUrl}"">COLLABORATE</a>";

                    string? destination = invite.InviteeEmail;

                    Company? btCompany = await _companyService.GetCompanyInfoAsync(companyId);

                    string? subject = $"JABugTracker: {btCompany.Name} Invite";

                    await _emailService.SendEmailAsync(destination!, subject, body);

                    //Save the invite in the DB
                    invite.CompanyToken = guid;
                    invite.CompanyId = companyId;
                    invite.InviteDate = DataUtility.GetPostGresDate(DateTime.Now);
                    invite.InvitorId = _userManager.GetUserId(User);
                    invite.IsValid = true;

                    await _inviteService.AddNewInviteAsync(invite);


                    return RedirectToAction("PortoIndex", "Home");
                }
                catch (Exception)
                {

                    throw;
                }


            }


            ViewData["ProjectId"] = new SelectList(_context.Projects.Where(p => p.CompanyId == companyId), "Id", "Description");
            return View(invite);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessInvite(string token, string email, string company)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(company))
            {
                return NotFound();
            }

            Guid companyToken = Guid.Parse(_protector.Unprotect(token));
            string? inviteeEmail = _protector.Unprotect(email);
            int companyId = int.Parse(_protector.Unprotect(company));

            try
            {
                Invite? invite = await _inviteService.GetInviteAsync(companyToken, inviteeEmail, companyId);
                if (invite != null)
                {
                    return View(invite);
                }
                return NotFound();
            }
            catch (Exception)
            {

                throw;
            }



        }


    }
}
