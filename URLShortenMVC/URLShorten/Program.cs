using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Policy;
using System.Threading.RateLimiting;
using URLShorten.Data;
using URLShorten.Data.IdentityEntities;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddValidatorsFromAssemblyContaining<IValidator>();

// ✅ Register DbContext with connection string
builder.Services.AddDbContext<UrlShortenDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UrlLinksConnection")));

//builder.Services.AddDefaultIdentity<UrlLinksUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<UrlShortenIdentityDbContext>();

builder.Services.AddDbContext<UrlShortenIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UrlLinksIdentityConnection")));

builder.Services.AddIdentity<UrlLinksUser, UrlLinksRole>(options =>
{
    //options.SignIn.RequireConfirmedAccount = true;
    // Password settings
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(0);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    // User settings
    options.User.RequireUniqueEmail = true;
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<UrlShortenIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    //options.LoginPath = "/Identity/Account/Login";
    options.LoginPath = "/Authentication/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Giới hạn theo IP, mỗi IP tối đa 5 request/giây
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5, // tối đa 5 request
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 5,
            AutoReplenishment = true
        });
    });
    options.RejectionStatusCode = 429; // Too Many Requests
});

//// ✅ Register MVC controllers + views
//builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed admin user and role
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UrlLinksUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UrlLinksRole>>();

    // Ensure Admin role exists
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new UrlLinksRole { Name = "Admin" });
    }

    // Ensure Admin user exists
    var adminEmail = "Admin1@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new UrlLinksUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    else
    {
        // Ensure user is in Admin role
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// ✅ Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ (Optional) Authentication if used
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// ✅ Custom redirect route for shortened URLs
app.MapControllerRoute(
    name: "redirect",
    pattern: "r/{slug}",
    defaults: new { controller = "UrlLinks", action = "RedirectToOriginal" });

app.MapGet("/", context =>
{
    context.Response.Redirect("/UrlLinks/Create");
    return Task.CompletedTask;
});

//// ✅ Default MVC route
app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=UrlLinks}/{action=Create}/{id?}");
//pattern: "{controller=Authentication}/{action=Login}/{id?}";

app.MapRazorPages();

app.Run();
