using Convocation.DataAccess;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ConvocationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Convocation.Session";
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ConvocationAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Restore session values from cookie claims if session is empty
app.Use(async (context, next) =>
{
    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
    {
        if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
        {
            context.Session.SetString("UserId",
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");

            context.Session.SetString("UserEmail",
                context.User.FindFirst(ClaimTypes.Email)?.Value ?? "");

            context.Session.SetString("Role",
                context.User.FindFirst(ClaimTypes.Role)?.Value ?? "");

            context.Session.SetString("FullName",
                context.User.FindFirst(ClaimTypes.Name)?.Value ?? "");
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();