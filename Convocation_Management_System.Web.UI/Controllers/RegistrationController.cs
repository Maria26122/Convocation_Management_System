using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class RegistrationController : BaseController
    {
        private readonly ConvocationDbContext _context;

        public RegistrationController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            IQueryable<Registration> query = _context.Registration
                .Include(r => r.Participant)
                    .ThenInclude(p => p.UserAccount)
                .Include(r => r.Event);

            if (IsParticipant())
            {
                var participantId = await CurrentParticipantIdAsync();
                query = query.Where(r => r.ParticipantId == participantId);
            }
            else if (!IsAdmin() && !IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            var Registration = await query
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            return View(Registration);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var registration = await _context.Registration
                .Include(r => r.Participant)
                    .ThenInclude(p => p.UserAccount)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id.Value);

            if (registration == null)
                return NotFound();

            if (IsParticipant())
            {
                var participantId = await CurrentParticipantIdAsync();

                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }

            return View(registration);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? eventId)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            var events = await _context.Event
                .Select(e => new
                {
                    e.EventId,
                    e.EventTitle,
                    e.EventDate,
                    e.BaseFee,
                    e.GuestFee
                })
                .ToListAsync();

            ViewBag.Event = events;
            ViewBag.SelectedEventId = eventId;

            var model = new Registration
            {
                EventId = eventId ?? 0,
                GuestCount = 0,
                TotalAmount = 0,
                RegistrationStatus = "Pending",
                RegistrationDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Registration registration)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            var selectedEvent = await _context.Event
                .FirstOrDefaultAsync(e => e.EventId == registration.EventId);

            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "Invalid event.");
                return View(registration);
            }

            if (registration.GuestCount < 0)
                registration.GuestCount = 0;

            registration.GuestCount = registration.GuestCount;

            var eventData = await _context.Event.FindAsync(registration.EventId);

            registration.TotalAmount =
                eventData.BaseFee +
                (registration.GuestCount * eventData.GuestFee);

            registration.RegistrationStatus = "Pending";

            // 🔥 SERVER SIDE CALCULATION (IMPORTANT)
            registration.TotalAmount =
                (selectedEvent.BaseFee = 0) +
                (registration.GuestCount * (selectedEvent.GuestFee=0));

            registration.RegistrationStatus = "Pending";
            registration.RegistrationDate = DateTime.Now;

            _context.Registration.Add(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration created successfully.";

            return RedirectToAction("PayNow", "Payment", new { registrationId = registration.RegistrationId });

          

           
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var registration = await _context.Registration.FindAsync(id.Value);

            if (registration == null)
                return NotFound();

            if (IsParticipant())
            {
                var participantId = await CurrentParticipantIdAsync();

                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }
            else if (!IsAdmin() && !IsStaff())
            {
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(registration.ParticipantId, registration.EventId);
            ViewBag.SelectedEventId = registration.EventId;

            return View(registration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Registration model)
        {
            if (id != model.RegistrationId)
                return NotFound();

            var registration = await _context.Registration
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            // ONLY UPDATE SAFE FIELDS
            registration.EventId = model.EventId;
            registration.RegistrationStatus = model.RegistrationStatus;
            registration.RegistrationDate = model.RegistrationDate;

            // DO NOT TOUCH ParticipantId unless needed

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var registration = await _context.Registration
                .Include(r => r.Participant)
                .ThenInclude(p => p.UserAccount)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id.Value);

            if (registration == null)
                return NotFound();

            if (IsParticipant())
            {
                var participantId = await CurrentParticipantIdAsync();

                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }
            else if (!IsAdmin())
            {
                return RedirectToAction(nameof(Index));
            }

            return View(registration);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            var registration = await _context.Registration.FindAsync(id);

            if (registration == null)
                return RedirectToAction(nameof(Index));

            if (IsParticipant())
            {
                var participantId = await CurrentParticipantIdAsync();

                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }
            else if (!IsAdmin())
            {
                return RedirectToAction(nameof(Index));
            }

            _context.Registration.Remove(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<int> CurrentParticipantIdAsync()
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out int userId))
                return 0;

            var participant = await _context.Participant
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            return participant?.ParticipantId ?? 0;
        }

        private async Task LoadDropdownsAsync(object? selectedParticipant = null, object? selectedEvent = null)
        {
            if (IsAdmin() || IsStaff())
            {
                ViewBag.ParticipantId = new SelectList(
                    await _context.Participant
                        .Include(p => p.UserAccount)
                        .Select(p => new
                        {
                            p.ParticipantId,
                            DisplayText = p.StudentId + " - " +
                                          (p.UserAccount != null
                                              ? p.UserAccount.FullName
                                              : p.Department)
                        })
                        .ToListAsync(),
                    "ParticipantId",
                    "DisplayText",
                    selectedParticipant
                );
            }
            else
            {
                var participantId = await CurrentParticipantIdAsync();

                ViewBag.ParticipantId = new SelectList(
                    await _context.Participant
                        .Where(p => p.ParticipantId == participantId)
                        .Select(p => new
                        {
                            p.ParticipantId,
                            DisplayText = p.StudentId + " - " + p.Department
                        })
                        .ToListAsync(),
                    "ParticipantId",
                    "DisplayText",
                    selectedParticipant
                );
            }

            var now = DateTime.Now;

            var EventQuery = _context.Event.AsQueryable();

            if (IsParticipant())
            {
                EventQuery = EventQuery.Where(e =>
                    e.IsActive &&
                    e.RegistrationtartDate <= now &&
                    e.RegistrationEndDate >= now);
            }

            ViewBag.Event = await EventQuery
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.EventTitle,
                    e.EventDate,
                    e.BaseFee,
                    e.GuestFee
                })
                .ToListAsync();

            ViewBag.SelectedEventId = selectedEvent;
        }
    }
}