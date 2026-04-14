using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class EventController : BaseController
    {
        private readonly ConvocationDbContext _context;

        public EventController(ConvocationDbContext context)
        {
            _context = context;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            if (!IsAdmin() && !IsEventManager())
                return RedirectToAction("Login", "Account");

            var events = await _context.Events
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            return View(events);
        }
        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return View(eventItem);
        }

        // GET: Event/Create
        public IActionResult Create()
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(Event model)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Events.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: Event/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            if (id != model.EventId)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Events.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: Event/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }
        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "admin" && role != "eventmanager")
                return RedirectToAction("Login", "Account");

            var eventItem = await _context.Events.FindAsync(id);

            if (eventItem != null)
            {
                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.EventId == id);
        }
    }
}