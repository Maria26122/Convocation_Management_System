using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly ConvocationDbContext _context;

        public RegistrationController(ConvocationDbContext context)
        {
            _context = context;
        }

        // GET: Registration
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (HttpContext.Session.GetString("Role") != "Participant")
            {
                return RedirectToAction("Login", "Account");
            }
            var registrations = await _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            return View(registrations);
        }

        // GET: Registration/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
            {
                return NotFound();
            }

            return View(registration);
        }

        // GET: Registration/Create
        public IActionResult Create()
        {
            LoadDropdowns();

            var registration = new Registration
            {
                RegistrationDate = DateTime.Now,
                RegistrationStatus = "Pending",
                GuestCount = 0,
                TotalAmount = 0
            };

            return View(registration);
        }

        // POST: Registration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Registration registration)
        {
            if (registration.GuestCount < 0)
            {
                ModelState.AddModelError("GuestCount", "Guest count cannot be negative.");
            }

            bool alreadyExists = await _context.Registrations
                .AnyAsync(r => r.ParticipantId == registration.ParticipantId &&
                               r.EventId == registration.EventId);

            if (alreadyExists)
            {
                ModelState.AddModelError("", "This participant is already registered for the selected event.");
            }

            var selectedEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == registration.EventId);

            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "Selected event is invalid.");
            }
            else
            {
                registration.TotalAmount = selectedEvent.BaseFee + (registration.GuestCount * selectedEvent.GuestFee);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    registration.RegistrationDate = DateTime.Now;

                    if (string.IsNullOrWhiteSpace(registration.RegistrationStatus))
                    {
                        registration.RegistrationStatus = "Pending";
                    }

                    _context.Registrations.Add(registration);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration saved successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                }
            }

            LoadDropdowns(registration.ParticipantId, registration.EventId);
            return View(registration);
        }

        // POST: Registration/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Registration registration)
        {
            if (id != registration.RegistrationId)
            {
                return NotFound();
            }

            if (registration.GuestCount < 0)
            {
                ModelState.AddModelError("GuestCount", "Guest count cannot be negative.");
            }

            if (registration.TotalAmount < 0)
            {
                ModelState.AddModelError("TotalAmount", "Total amount cannot be negative.");
            }

            bool duplicateExists = await _context.Registrations
                .AnyAsync(r => r.RegistrationId != registration.RegistrationId &&
                               r.ParticipantId == registration.ParticipantId &&
                               r.EventId == registration.EventId);

            if (duplicateExists)
            {
                ModelState.AddModelError("", "This participant is already registered for the selected event.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(registration.RegistrationStatus))
                    {
                        registration.RegistrationStatus = "Pending";
                    }

                    _context.Update(registration);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RegistrationExists(registration.RegistrationId))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                }
            }

            LoadDropdowns(registration.ParticipantId, registration.EventId);
            return View(registration);
        }

        // GET: Registration/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
            {
                return NotFound();
            }

            return View(registration);
        }

        // POST: Registration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var registration = await _context.Registrations.FindAsync(id);

            if (registration != null)
            {
                _context.Registrations.Remove(registration);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registration deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RegistrationExists(int id)
        {
            return _context.Registrations.Any(r => r.RegistrationId == id);
        }

        private void LoadDropdowns(object? selectedParticipant = null, object? selectedEvent = null)
        {
            ViewBag.ParticipantId = new SelectList(
                _context.Participants
                    .Select(p => new
                    {
                        p.ParticipantId,
                        DisplayText = p.StudentId + " - " + p.Department
                    })
                    .ToList(),
                "ParticipantId",
                "DisplayText",
                selectedParticipant
            );

            ViewBag.Events = _context.Events
                .Select(e => new
                {
                    e.EventId,
                    e.EventTitle,
                    e.EventDate,
                    e.BaseFee,
                    e.GuestFee
                })
                .ToList();

            ViewBag.EventId = new SelectList(
                _context.Events
                    .Select(e => new
                    {
                        e.EventId,
                        DisplayText = e.EventTitle + " - " + e.EventDate.ToString("dd MMM yyyy")
                    })
                    .ToList(),
                "EventId",
                "DisplayText",
                selectedEvent
            );
        }
    }
}
