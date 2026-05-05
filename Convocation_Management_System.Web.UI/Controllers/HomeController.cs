using Convocation.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ConvocationDbContext _context;

        public HomeController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            var upcomingEvents = await _context.Event
                .Where(e => e.IsActive && e.EventDate >= now)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return View(upcomingEvents);
        }
    }
}