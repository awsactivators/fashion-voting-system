// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using FashionVote.Data;
// using FashionVote.Services;
// using Microsoft.AspNetCore.Identity.UI.Services;

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlite(connectionString));

// builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// builder.Services.AddSingleton<IEmailSender, EmailSender>();

// // builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
// //     .AddEntityFrameworkStores<ApplicationDbContext>();

// builder.Services.AddControllersWithViews();

// builder.Services.AddRazorPages();

// /// Add Identity with Role Support
// builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
// {
//     options.SignIn.RequireConfirmedAccount = false; // Change if you require email confirmation
// })
// .AddEntityFrameworkStores<ApplicationDbContext>()
// .AddDefaultTokenProviders();

// .AddRoles<IdentityRole>();  // Enable Role Management

// var app = builder.Build();


// // Seed Roles and Admin User
// using (var scope = app.Services.CreateScope())
// {
//     var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

//     string adminEmail = "admin@example.com"; // CHANGE THIS TO YOUR ADMIN EMAIL
//     string adminPassword = "Admin@123"; // Ensure you know the password

//     // Ensure Admin Role Exists
//     if (!await roleManager.RoleExistsAsync("Admin"))
//     {
//         await roleManager.CreateAsync(new IdentityRole("Admin"));
//     }

//     // Assign Admin Role to User
//     var adminUser = await userManager.FindByEmailAsync(adminEmail);
//     if (adminUser != null && !(await userManager.IsInRoleAsync(adminUser, "Admin")))
//     {
//         await userManager.AddToRoleAsync(adminUser, "Admin");
//     }
// };



// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseMigrationsEndPoint();
// }
// else
// {
//     app.UseExceptionHandler("/Home/Error");
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     app.UseHsts();
// }


// app.UseHttpsRedirection();
// app.UseStaticFiles();

// app.UseRouting();

// app.UseAuthorization();
// app.UseAuthentication();

// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}");
// app.MapRazorPages();

// app.UseEndpoints(endpoints =>
// {
//     endpoints.MapControllerRoute(
//         name: "default",
//         pattern: "{controller=Home}/{action=Index}/{id?}");
//     endpoints.MapRazorPages();
// });

// app.Run();



using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FashionVote.Data;
using FashionVote.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ✅ Register Email Sender Service
builder.Services.AddSingleton<IEmailSender, EmailSender>();

// ✅ Corrected: Add Identity with Role Support
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // Disable email confirmation for now
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddRoles<IdentityRole>(); // ✅ Moved `.AddRoles<IdentityRole>()` to the right place

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ✅ Seed Roles and Admin User on Startup
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string adminEmail = "admin@fashionvote.com"; // 🔹 CHANGE THIS TO YOUR ADMIN EMAIL
    string adminPassword = "Admin@123"; // 🔹 Use a strong password

    // ✅ Ensure Admin Role Exists
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // ✅ Ensure Participant Role Exists
    if (!await roleManager.RoleExistsAsync("Participant"))
    {
        await roleManager.CreateAsync(new IdentityRole("Participant"));
    }

    // ✅ Assign Admin Role to an Admin User
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
