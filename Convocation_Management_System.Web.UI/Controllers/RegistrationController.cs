using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Helpers;
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
            if (!this.LoggedIn())
                return RedirectToAction("Login", "Account");

            IQueryable<Registration> query = _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event);

            if (this.IsParticipant())
            {
                var participantId = await this.CurrentParticipantId(_context);
                query = query.Where(r => r.ParticipantId == participantId);
            }
            else if (!this.IsAdmin() && !this.IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            var registrations = await query
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            return View(registrations);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!this.LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var registration = await _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            if (this.IsParticipant())
            {
                var participantId = await this.CurrentParticipantId(_context);
                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }

            return View(registration);
        }

        private async Task<int> CurrentParticipantId(ConvocationDbContext context)
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
                return 0;

            if (!int.TryParse(userIdString, out int userId))
                return 0;

            var participant = await context.Participants
                .FirstOrDefaultAsync(p => p.UserAccountId == userId);

            return participant?.ParticipantId ?? 0;
        }

        public async Task<IActionResult> Create()
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "student")
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            await LoadDropdownsAsync(_context.Events.Where(e => e.IsActive));

            var registration = new Registration
            {
                RegistrationDate = DateTime.Now,
                RegistrationStatus = "Pending",
                GuestCount = 0,
                TotalAmount = 0
            };

           
            var userIdString = HttpContext.Session.GetString("UserId");

            if ((role ?? "").Trim().ToLower() == "student" && int.TryParse(userIdString, out int userId))
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.UserAccountId == userId);

                if (participant != null)
                    registration.ParticipantId = participant.ParticipantId;
            }

            return View(registration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Registration registration)
        {
            var role = (HttpContext.Session.GetString("Role") ?? "").ToLower();

            if (role != "student")
                return RedirectToAction("Login", "Account");

            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

          
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!int.TryParse(userIdString, out int userId))
                return RedirectToAction("Login", "Account");

            // For participant login, auto-assign ParticipantId
            if ((role ?? "").Trim().ToLower() == "student")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.UserAccountId == userId);

                if (participant == null)
                {
                    ModelState.AddModelError("", "Participant profile not found for this user.");
                    await LoadDropdownsAsync(registration.ParticipantId, registration.EventId);
                    return View(registration);
                }

                registration.ParticipantId = participant.ParticipantId;

                // very important
                ModelState.Remove("ParticipantId");
            }

            if (registration.GuestCount < 0)
                ModelState.AddModelError("GuestCount", "Guest count cannot be negative.");

            bool alreadyExists = await _context.Registrations
                .AnyAsync(r => r.ParticipantId == registration.ParticipantId &&
                               r.EventId == registration.EventId);

            if (alreadyExists)
                ModelState.AddModelError("", "This participant is already registered for the selected event.");

            var selectedEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == registration.EventId);

            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "Selected event is invalid.");
            }
            else
            {
                if (registration.GuestCount > selectedEvent.MaxGuestAllowed)
                    ModelState.AddModelError("GuestCount", $"Maximum allowed guest is {selectedEvent.MaxGuestAllowed}.");

                registration.TotalAmount = selectedEvent.BaseFee + (registration.GuestCount * selectedEvent.GuestFee);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(registration.ParticipantId, registration.EventId);
                return View(registration);
            }

            registration.RegistrationDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(registration.RegistrationStatus))
                registration.RegistrationStatus = "Pending";

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!this.LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
                return NotFound();

            if (this.IsParticipant())
            {
                var participantId = await this.CurrentParticipantIdAsync(_context);
                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }
            else if (!this.IsAdmin() && !this.IsStaff())
            {
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(registration.ParticipantId, registration.EventId);
            return View(registration);
        }

        private async Task<int> CurrentParticipantIdAsync(ConvocationDbContext context)
        {
            return await CurrentParticipantId(context);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Registration registration)
        {
            if (!this.LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id != registration.RegistrationId)
                return NotFound();

            var existing = await _context.Registrations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (existing == null)
                return NotFound();

            if (this.IsParticipant())
            {
                var participantId = await this.CurrentParticipantIdAsync(_context);
                if (existing.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));

                registration.ParticipantId = existing.ParticipantId;
                registration.RegistrationStatus = existing.RegistrationStatus;
            }
            else if (!this.IsAdmin() && !this.IsStaff())
            {
                return RedirectToAction(nameof(Index));
            }

            if (registration.GuestCount < 0)
                ModelState.AddModelError("GuestCount", "Guest count cannot be negative.");

            bool duplicateExists = await _context.Registrations
                .AnyAsync(r => r.RegistrationId != registration.RegistrationId &&
                               r.ParticipantId == registration.ParticipantId &&
                               r.EventId == registration.EventId);

            if (duplicateExists)
                ModelState.AddModelError("", "This participant is already registered for the selected event.");

            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == registration.EventId);
            if (selectedEvent == null)
            {
                ModelState.AddModelError("EventId", "Selected event is invalid.");
            }
            else
            {
                if (registration.GuestCount > selectedEvent.MaxGuestAllowed)
                    ModelState.AddModelError("GuestCount", $"Maximum allowed guest is {selectedEvent.MaxGuestAllowed}.");

                registration.TotalAmount = selectedEvent.BaseFee + (registration.GuestCount * selectedEvent.GuestFee);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(registration.ParticipantId, registration.EventId);
                return View(registration);
            }

            try
            {
                registration.RegistrationDate = existing.RegistrationDate;
                _context.Update(registration);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Registrations.Any(r => r.RegistrationId == registration.RegistrationId))
                    return NotFound();

                throw;
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!this.LoggedIn())
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var registration = await _context.Registrations
                .Include(r => r.Participant)
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            if (this.IsParticipant())
            {
                var participantId = await this.CurrentParticipantIdAsync(_context);
                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }
            else if (!this.IsAdmin())
            {
                return RedirectToAction(nameof(Index));
            }

            return View(registration);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!this.LoggedIn())
                return RedirectToAction("Login", "Account");

            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
                return RedirectToAction(nameof(Index));

            if (this.IsParticipant())
            {
                var participantId = await this.CurrentParticipantIdAsync(_context);
                if (registration.ParticipantId != participantId)
                    return RedirectToAction(nameof(Index));
            }
            else if (!this.IsAdmin())
            {
                return RedirectToAction(nameof(Index));
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownsAsync(object? selectedParticipant = null, object? selectedEvent = null)
        {
            if (this.IsAdmin() || this.IsStaff())
            {
                ViewBag.ParticipantId = new SelectList(
                    await _context.Participants
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
            else
            {
                var participantId = await this.CurrentParticipantIdAsync(_context);

                ViewBag.ParticipantId = new SelectList(
                    await _context.Participants
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

            ViewBag.Events = await _context.Events
                .Select(e => new
                {
                    e.EventId,
                    e.EventTitle,
                    e.EventDate,
                    e.BaseFee,
                    e.GuestFee
                })
                .ToListAsync();
            ViewBag.EventId = new SelectList(
                await _context.Events
                    .Where(e => e.IsActive)
                    .Select(e => new
                    {
                        e.EventId,
                        DisplayText = e.EventTitle + " - " + e.EventDate.ToString("dd MMM yyyy")
                    })
                    .ToListAsync(),
                "EventId",
                "DisplayText",
                selectedEvent
            );
        }
    }
}