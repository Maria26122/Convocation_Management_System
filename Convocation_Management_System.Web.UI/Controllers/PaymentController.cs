using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ConvocationDbContext _context;

        public PaymentController(ConvocationDbContext context)
        {
            _context = context;
        }

        // GET: Payment
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin" && role != "Staff")
            {
                return RedirectToAction("Login", "Account");
            }
            var payments = await _context.Payments
                .Include(p => p.Registration)
                .ThenInclude(r => r.Participant)
                .Include(p => p.Registration)
                .ThenInclude(r => r.Event)
                .OrderByDescending(p => p.PaymentId)
                .ToListAsync();

            return View(payments ?? new List<Payment>());
        }

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Registration)
                .ThenInclude(r => r.Participant)
                .Include(p => p.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // GET: Payment/Create
        public IActionResult Create()
        {
            LoadRegistrationDropdown();
            return View();
        }

        // POST: Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            if (!ModelState.IsValid)
            {
                LoadRegistrationDropdown(payment.RegistrationId);
                return View(payment);
            }

            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await UpdateRegistrationStatusFromPayment(payment);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                LoadRegistrationDropdown(payment.RegistrationId);
                return View(payment);
            }
        }

        // GET: Payment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            LoadRegistrationDropdown(payment.RegistrationId);
            return View(payment);
        }

        // POST: Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment payment)
        {
            if (id != payment.PaymentId) return NotFound();

            if (!ModelState.IsValid)
            {
                LoadRegistrationDropdown(payment.RegistrationId);
                return View(payment);
            }

            try
            {
                _context.Update(payment);
                await _context.SaveChangesAsync();

                await UpdateRegistrationStatusFromPayment(payment);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(payment.PaymentId))
                    return NotFound();

                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.InnerException?.Message ?? ex.Message);
                LoadRegistrationDropdown(payment.RegistrationId);
                return View(payment);
            }
        }

        // GET: Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Registration)
                .ThenInclude(r => r.Participant)
                .Include(p => p.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

        private void LoadRegistrationDropdown(object? selectedRegistration = null)
        {
            var registrationList = _context.Registrations
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
                registrationList,
                "RegistrationId",
                "DisplayText",
                selectedRegistration
            );
        }

        private async Task UpdateRegistrationStatusFromPayment(Payment payment)
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.RegistrationId == payment.RegistrationId);

            if (registration == null)
                return;

            if (payment.PaymentStatus == "Paid")
            {
                registration.RegistrationStatus = "Confirmed";
            }
            else if (payment.PaymentStatus == "Failed")
            {
                registration.RegistrationStatus = "Rejected";
            }
            else
            {
                registration.RegistrationStatus = "Pending";
            }

            _context.Registrations.Update(registration);
            await _context.SaveChangesAsync();
        }
    }
}