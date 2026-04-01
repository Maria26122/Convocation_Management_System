using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class GuestController : Controller
    {
        private readonly ConvocationDbContext _context;

        public GuestController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var guests = await _context.Guests
                .Include(g => g.Registration)
                .ThenInclude(r => r.Participant)
                .Include(g => g.Registration)
                .ThenInclude(r => r.Event)
                .OrderByDescending(g => g.GuestId)
                .ToListAsync();

            return View(guests ?? new List<Guest>());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var guest = await _context.Guests
                .Include(g => g.Registration)
                .ThenInclude(r => r.Participant)
                .Include(g => g.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(g => g.GuestId == id);

            if (guest == null) return NotFound();

            return View(guest);
        }

        public IActionResult Create()
        {
            LoadRegistrationDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guest guest)
        {
            if (!ModelState.IsValid)
            {
                LoadRegistrationDropdown(guest.RegistrationId);
                return View(guest);
            }

            try
            {
                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Save failed: " + ex.Message);
                LoadRegistrationDropdown(guest.RegistrationId);
                return View(guest);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            LoadRegistrationDropdown(guest.RegistrationId);
            return View(guest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Guest guest)
        {
            if (id != guest.GuestId) return NotFound();

            if (!ModelState.IsValid)
            {
                LoadRegistrationDropdown(guest.RegistrationId);
                return View(guest);
            }

            try
            {
                _context.Update(guest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Update failed: " + ex.Message);
                LoadRegistrationDropdown(guest.RegistrationId);
                return View(guest);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var guest = await _context.Guests
                .Include(g => g.Registration)
                .ThenInclude(r => r.Participant)
                .Include(g => g.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(g => g.GuestId == id);

            if (guest == null) return NotFound();

            return View(guest);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest != null)
            {
                _context.Guests.Remove(guest);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private void LoadRegistrationDropdown(object? selectedRegistration = null)
        {
            var registrations = _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .AsEnumerable()
                .Select(r => new
                {
                    RegistrationId = r.RegistrationId,
                    DisplayText = $"Reg #{r.RegistrationId} - {(r.Participant != null ? r.Participant.StudentId : "No Student")} - {(r.Event != null ? r.Event.EventTitle : "No Event")}"
                })
                .ToList();

            ViewBag.RegistrationId = new SelectList(
                registrations,
                "RegistrationId",
                "DisplayText",
                selectedRegistration
            );
        }
    }
}