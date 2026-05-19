using Convocation.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        // ✅ FIXED METHOD (NOW INSIDE CLASS)
        protected void LoadRegistrationDropdown(ConvocationDbContext _context, object? selected = null)
        {
            var data = _context.Registration
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .Select(r => new
                {
                    r.RegistrationId,
                    DisplayText = $"Reg {r.RegistrationId} - {r.Participant.StudentId} - {r.Event.EventTitle}"
                })
                .ToList();

            ViewBag.RegistrationId = new SelectList(data, "RegistrationId", "DisplayText", selected);
        }
    }
}