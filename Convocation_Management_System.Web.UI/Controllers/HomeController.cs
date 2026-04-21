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
            var events = await _context.Events
                .Where(e => e.IsActive)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return View(events);
        }
    }
}