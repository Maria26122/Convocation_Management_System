using Convocation.DataAccess;
using Convocation.Entities;
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

        // GET: DistributionLog
        public async Task<IActionResult> Index()
        {
            var logs = await _context.DistributionLog
                .Include(d => d.Event)
                .Include(d => d.Participant)
                    .ThenInclude(p => p.UserAccount)
                .OrderByDescending(d => d.ActionDate)
                .ToListAsync();

            return View(logs);
        }
        // GET: DistributionLog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var log = await _context.DistributionLog
                .Include(d => d.Event)
                .Include(d => d.Participant)
                    .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(m => m.DistributionLogId == id);

            if (log == null)
                return NotFound();

            return View(log);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDistributionPlan(int eventId, string notes)
        {
            var exists = await _context.DistributionLog
                .AnyAsync(d => d.EventId == eventId && d.ActionType == "PLAN");

            if (exists)
            {
                TempData["Error"] = "Distribution already created for this event.";
                return RedirectToAction("Index");
            }

            var plan = new DistributionLog
            {
                EventId = eventId,
                ActionType = "PLAN",
                ActionDate = DateTime.Now,
                Note = notes,
                Remarks = "Distribution plan created"
            };

            _context.DistributionLog.Add(plan);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Distribution plan created successfully.";
            return RedirectToAction("Index");
        }

        // GET: DistributionLog/Create
        public IActionResult Create()
        {
            ViewBag.ParticipantId = _context.Participant
                .Include(p => p.UserAccount)
                .Select(p => new SelectListItem
                {
                    Value = p.ParticipantId.ToString(),
                    Text = p.StudentId + " - " + p.UserAccount.FullName
                }).ToList();

            ViewBag.EventId = _context.Event
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventTitle
                }).ToList();

            ViewBag.DistributionTypes = new List<string>
    {
        "Food",
        "Kit",
        "Gown",
        "Certificate"
    };

            return View();
        }

        // POST: DistributionLog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionLog log)
        {
            if (!ModelState.IsValid)
                return View(log);

            log.ActionDate = DateTime.Now;

            // 🔥 FIX 1: prevent duplicate same action
            bool exists = await _context.DistributionLog.AnyAsync(d =>
                d.RegistrationId == log.RegistrationId &&
                d.ActionType == log.ActionType);

            if (exists)
            {
                ModelState.AddModelError("", "This action already exists for this student.");
                return View(log);
            }

            _context.DistributionLog.Add(log);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: DistributionLog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var log = await _context.DistributionLog.FindAsync(id);

            if (log == null)
                return NotFound();

            return View(log);
        }

        // POST: DistributionLog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DistributionLogId,RegistrationId,ParticipantId,ActionType,ActionDate,UserAccountId,Note,Remarks")] DistributionLog log)
        {
            if (id != log.DistributionLogId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(log);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DistributionLogExists((int)log.DistributionLogId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(log);
        }



        // GET: DistributionLog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var log = await _context.DistributionLog
                .Include(d => d.Event)
                .Include(d => d.Participant)
                    .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(m => m.DistributionLogId == id);

            if (log == null)
                return NotFound();

            return View(log);
        }

        // POST: DistributionLog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var log = await _context.DistributionLog.FindAsync(id);

            if (log != null)
            {
                _context.DistributionLog.Remove(log);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DistributionLogExists(int id)
        {
            return _context.DistributionLog.Any(e => e.DistributionLogId == id);
        }
        private async Task AddLog(int registrationId, int userId, string actionType, string note)
        {
            var log = new DistributionLog
            {
                RegistrationId = registrationId,
                UserAccountId = userId,
                ActionType = actionType,
                ActionDate = DateTime.Now,
                Note = note,
                Remarks = "Auto logged"
            };

            _context.DistributionLog.Add(log);
            await _context.SaveChangesAsync();
        }

    }
}