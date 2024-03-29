using JABugTracker.Data;
using JABugTracker.Extensions;
using JABugTracker.Models;
using JABugTracker.Services;
using JABugTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = DataUtility.GetConnectionString(builder.Configuration) ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<BTUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<BTUserClaimsPrincipalFactory>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();
    

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//Custom Services
builder.Services.AddScoped<IEmailSender, EmailService>();
builder.Services.AddScoped<IBTRoleService, BTRoleService>();
builder.Services.AddScoped<IBTFileService, BTFileService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IBTCompanyService, BTCompanyService>();
builder.Services.AddScoped<IBTTicketHistoryService, BTTicketHistoryService>();
builder.Services.AddScoped<IBTTicketCommentService, TicketCommentService>();
builder.Services.AddScoped<IBTNotificationService, NotificationService>();
builder.Services.AddScoped<IBTInviteService, InviteService>();

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));


builder.Services.AddMvc();

var app = builder.Build();
var scope = app.Services.CreateScope();
await DataUtility.ManageDataAsync(scope.ServiceProvider);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
