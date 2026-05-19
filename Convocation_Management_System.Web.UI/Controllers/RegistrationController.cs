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
            {
                if (eventId.HasValue && eventId.Value > 0)
                {
                    return RedirectToAction("Register", "Account", new { eventId = eventId.Value });
                }

                return RedirectToAction("Login", "Account");
            }

            if (!IsParticipant() && !IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            await LoadDropdownsAsync(null, eventId);

            var model = new Registration
            {
                EventId = eventId ?? 0,
                RegistrationDate = DateTime.Now,
                RegistrationStatus = "Pending",
                GuestCount = 0,
                TotalAmount = 0
            };

            if (IsParticipant())
            {
                model.ParticipantId = await CurrentParticipantIdAsync();

                if (model.ParticipantId == 0)
                {
                    TempData["Error"] = "Participant profile not found. Please complete your profile first.";
                    return RedirectToAction("Dashboard", "Participant");
                }
            }

            ViewBag.SelectedEventId = eventId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Registration registration)
        {
            if (!LoggedIn())
                return RedirectToAction("Login", "Account");

            if (!IsParticipant() && !IsAdmin() && !IsStaff())
                return RedirectToAction("Login", "Account");

            if (IsParticipant())
            {
                registration.ParticipantId = await CurrentParticipantIdAsync();
                ModelState.Remove("ParticipantId");
                ModelState.Remove("Participant");
            }

            ModelState.Remove("Event");

            if (registration.GuestCount < 0)
                ModelState.AddModelError("GuestCount", "Guest count cannot be negative.");

            var participantExists = await _context.Participant
                .AnyAsync(p => p.ParticipantId == registration.ParticipantId);

            if (!participantExists)
                ModelState.AddModelError("ParticipantId", "Invalid participant.");

            var selectedEvent = await _context.Event
                .FirstOrDefaultAsync(e => e.EventId == registration.EventId);

            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "Selected event is invalid.");
            }
            else
            {
                if (!selectedEvent.IsActive)
                    ModelState.AddModelError("EventId", "This event is not active.");

                var now = DateTime.Now;

                if (IsParticipant() &&
                    !(selectedEvent.RegistrationtartDate <= now &&
                      selectedEvent.RegistrationEndDate >= now))
                {
                    ModelState.AddModelError("EventId", "Registration is closed for this event.");
                }

                if (registration.GuestCount > selectedEvent.MaxGuestAllowed)
                    ModelState.AddModelError("GuestCount", $"Maximum allowed guest is {selectedEvent.MaxGuestAllowed}.");

                registration.TotalAmount = selectedEvent.BaseFee + (registration.GuestCount * selectedEvent.GuestFee);
            }

            bool alreadyExists = await _context.Registration
                .AnyAsync(r => r.ParticipantId == registration.ParticipantId &&
                               r.EventId == registration.EventId);

            if (alreadyExists)
                ModelState.AddModelError("", "This participant is already registered for the selected event.");

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(registration.ParticipantId, registration.EventId);
                ViewBag.SelectedEventId = registration.EventId;
                return View(registration);
            }

            registration.RegistrationDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(registration.RegistrationStatus))
                registration.RegistrationStatus = "Pending";

            _context.Registration.Add(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration saved successfully. Please complete payment.";

            if (IsParticipant())
            {
                if (registration.TotalAmount <= 0)
                {
                    TempData["Error"] = "Invalid amount.";
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction("PayNow", "Payment", new { registrationId = registration.RegistrationId });
            }

            return RedirectToAction(nameof(Index));
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