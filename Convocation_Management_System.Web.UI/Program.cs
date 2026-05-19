using Convocation.DataAccess;
using Convocation_Management_System.Web.UI;
using Convocation_Management_System.Web.UI.Helpers;
using Convocation_Management_System.Web.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// =========================
// SERVICES
// =========================
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddScoped<SSLCommercePayment>();
builder.Services.AddScoped<QrGeneratorService>();

// =========================
// DB
// =========================
builder.Services.AddDbContext<ConvocationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================
// SESSION
// =========================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Convocation.Session";
});

// =========================
// AUTH
// =========================
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

builder.Services.AddAuthorization();

var app = builder.Build();

// =========================
// ERROR HANDLING
// =========================
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


// =========================
// SAFE SESSION RESTORE (FIXED)
// =========================
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        var name = context.User.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrWhiteSpace(userId) &&
            string.IsNullOrWhiteSpace(context.Session.GetString("UserId")))
        {
            context.Session.SetString("UserId", userId);
            context.Session.SetString("UserEmail", email ?? "");
            context.Session.SetString("Role", role ?? "");
            context.Session.SetString("FullName", name ?? "");
        }
    }

    await next();
});

// =========================
// ROUTES
// =========================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();