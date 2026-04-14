using Microsoft.AspNetCore.Http;

namespace Convocation_Management_System.Web.UI.Helpers
{
    public static class SessionHelper
    {
        public static void SetUserSession(HttpContext httpContext, int userId, string email, string role, string fullName)
        {
            httpContext.Session.SetString("UserId", userId.ToString());
            httpContext.Session.SetString("UserEmail", email ?? "");
            httpContext.Session.SetString("Role", role ?? "");
            httpContext.Session.SetString("FullName", fullName ?? "");
        }

        public static void ClearSession(HttpContext httpContext)
        {
            httpContext.Session.Clear();
        }

        public static string? GetRole(HttpContext httpContext)
        {
            return httpContext.Session.GetString("Role");
        }

        public static int GetUserId(HttpContext httpContext)
        {
            string? id = httpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(id) ? 0 : int.Parse(id);
        }

        public static string? GetUserEmail(HttpContext httpContext)
        {
            return httpContext.Session.GetString("UserEmail");
        }

        public static string? GetFullName(HttpContext httpContext)
        {
            return httpContext.Session.GetString("FullName");
        }
    }
}