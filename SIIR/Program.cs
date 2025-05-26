using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SIIR.Data;
using SIIR.DataAccess.Data.Repository.IRepository;
using SIIR.Models;
using SIIR.DataAccess.Data.Repository;
using Microsoft.AspNetCore.Identity.UI.Services;
using SIIR.Utilities;
using System.Globalization;
using Microsoft.AspNetCore.Localization;


var builder = WebApplication.CreateBuilder(args);

// Configura la cultura para espa�ol
var cultureInfo = new CultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.
/*var connectionString = builder.Configuration.GetConnectionString("ConexionSQL") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));*/
var connectionString = builder.Configuration.GetConnectionString("ConexionPostgres") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString
    ,b => b.MigrationsAssembly("SIIR")));
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseInMemoryDatabase("DummyDatabase"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
        options => options.SignIn.RequireConfirmedAccount = false
        )
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>();

// sessions must expire after 15 minutes of inactivity
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// Default Lockout settings.
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

// authentication must be done using cookies
builder.Services.ConfigureApplicationCookie(options =>
{
	options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
	options.LoginPath = "/Identity/Account/Login";
	options.LogoutPath = "/Identity/Account/Logout";
	options.AccessDeniedPath = "/Identity/Account/AccessDenied";
	options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddTransient<IEmailSender, EmailSender>(i =>
    new EmailSender(
        builder.Configuration["EmailSettings:SmtpServer"],
        int.Parse(builder.Configuration["EmailSettings:SmtpPort"]),
        builder.Configuration["EmailSettings:FromEmailAddress"],
        builder.Configuration["EmailSettings:FromEmailPassword"]
    ));

//Contenedor de Trabajo
builder.Services.AddScoped<IContenedorTrabajo, ContenedorTrabajo>();

//PdfQuest
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

var supportedCultures = new[] { new CultureInfo("es-ES") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("es-ES"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

/*
// Seed default users
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Ensure the database is created
    context.Database.EnsureCreated();

    // Ensure roles exist
    var roles = new[] { "Admin", "Coach", "Student" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create a default admin user
    string email_admin = "admin@example.com";
    string password_admin = "Admin123!";
    
    if (await userManager.FindByEmailAsync(email_admin) == null)
    {
        var user = new ApplicationUser { UserName = email_admin, Email = email_admin };
        var result = await userManager.CreateAsync(user, password_admin);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }

    // Create a default admin user
    string email_coach = "coach@example.com";
    string password_coach = "Coach123!";
    
    if (await userManager.FindByEmailAsync(email_coach) == null)
    {
        var user = new ApplicationUser { UserName = email_coach, Email = email_coach };
        var result = await userManager.CreateAsync(user, password_coach);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Coach");
        }
    }

    // Create a default admin user
    string email_student = "student@example.com";
    string password_student = "Student123!";
    
    if (await userManager.FindByEmailAsync(email_student) == null)
    {
        var user = new ApplicationUser { UserName = email_student, Email = email_student };
        var result = await userManager.CreateAsync(user, password_student);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Student");
        }
    }
    
    string email_target = "l20140959@queretaro.tecnm.mx"; // o cualquier correo existente
    string newPassword = "Coach123!";

    var existingUser = await userManager.FindByEmailAsync(email_target);
    if (existingUser != null)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        var result = await userManager.ResetPasswordAsync(existingUser, token, newPassword);

        if (result.Succeeded)
        {
            Console.WriteLine("Contraseña actualizada correctamente.");
        }
        else
        {
            Console.WriteLine("Error al cambiar contraseña:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }

    string email_target = "l20140957@queretaro.tecnm.mx"; // o cualquier correo existente
    string newPassword = "Student123!";

    var existingUser = await userManager.FindByEmailAsync(email_target);
    if (existingUser != null)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        var result = await userManager.ResetPasswordAsync(existingUser, token, newPassword);

        if (result.Succeeded)
        {
            Console.WriteLine("Contraseña actualizada correctamente.");
        }
        else
        {
            Console.WriteLine("Error al cambiar contraseña:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"- {error.Description}");
            }
        }
    }
}*/

app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Student}/{controller=Information}/{action=Index}/{id?}");

// Redirect to login page instead of any other page

app.MapGet("/", context =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.Run();
