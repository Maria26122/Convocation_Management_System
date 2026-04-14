using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            if (_context.DistributionLogs == null)
            {
                return View(new List<DistributionLog>());
            }

            var logs = await _context.DistributionLogs
                .Include(d => d.Participant!)           // null-forgiving: navigation can be null in model, but safe here
                .ThenInclude(p => p!.UserAccount!)      // null-forgiving for nested navigation
                .OrderByDescending(d => d.DistributedAt)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new DistributionLogCreateViewModel();

            if (_context.Participants != null)
            {
                vm.Participant = await _context.Participants
                    .Select(p => new SelectListItem
                    {
                        Value = p.ParticipantId.ToString(),
                        Text = p.StudentId + " - " + p.Department + " - " + p.Program
                    })
                    .ToListAsync();
            }
            else
            {
                vm.Participant = new List<SelectListItem>();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionLogCreateViewModel vm)
        {
            if (vm == null) return BadRequest();

            if (!ModelState.IsValid)
            {
                if (_context.Participants != null)
                {
                    vm.Participant = await _context.Participants
                        .Select(p => new SelectListItem
                        {
                            Value = p.ParticipantId.ToString(),
                            Text = p.StudentId + " - " + p.Department + " - " + p.Program
                        })
                        .ToListAsync();
                }
                else
                {
                    vm.Participant = new List<SelectListItem>();
                }

                return View(vm);
            }

            var log = new DistributionLog
            {
                ParticipantId = vm.ParticipantId,
                ItemName = vm.ItemName!,      // null-forgiving: model validation ensures ItemName is present
                DistributedAt = DateTime.Now,
                DistributedBy = vm.DistributedBy,
                Remarks = vm.Remarks
            };

            if (_context.DistributionLogs == null)
            {
                return Problem("Database set for DistributionLogs is not available.");
            }

            _context.DistributionLogs.Add(log);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Distribution log saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}