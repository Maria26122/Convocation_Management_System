using Microsoft.AspNetCore.Mvc;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class BaseController : Controller
    {
        protected bool LoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        protected string CurrentRole()
        {
            return (HttpContext.Session.GetString("Role") ?? "").Trim().ToLower();
        }

        protected bool IsAdmin()
        {
            return CurrentRole() == "admin";
        }

        protected bool IsEventManager()
        {
            return CurrentRole() == "eventmanager";
        }

        protected bool IsStaff()
        {
            return CurrentRole() == "staff" || CurrentRole() == "eventmanager";
        }

        protected bool IsParticipant()
        {
            return CurrentRole() == "student" || CurrentRole() == "participant";
        }
    }
}