using Convocation.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class DistributionLogController : Controller
    {
        private readonly ConvocationDbContext _context;

        public DistributionLogController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.DistributionLog
                .Include(x => x.UserAccount)
                .Include(x => x.Participant)
                .Include(x => x.Event)
                .Include(x => x.DistributionTask)
                .OrderByDescending(x => x.ActionDate)
                .ToListAsync();

            return View(logs);
        }
    }
}
