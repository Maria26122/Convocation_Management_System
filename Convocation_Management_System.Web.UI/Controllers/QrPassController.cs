using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class QrPassController : Controller
    {
        private readonly ConvocationDbContext _context;

        public QrPassController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var qrPasses = await _context.QrPasses
                .Include(q => q.Registration)
                .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                .ThenInclude(r => r.Event)
                .OrderByDescending(q => q.QrPassId)
                .ToListAsync();

            return View(qrPasses ?? new List<QrPass>());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var qrPass = await _context.QrPasses
                .Include(q => q.Registration)
                .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(q => q.QrPassId == id);

            if (qrPass == null) return NotFound();

            return View(qrPass);
        }

        public IActionResult Create()
        {
            LoadRegistrationDropdown();
            return View(new QrPass { IsUsed = true, IssuedAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QrPass qrPass)
        {
            if (string.IsNullOrWhiteSpace(qrPass.QrCodeText))
            {
                qrPass.QrCodeText = Guid.NewGuid().ToString("N").ToUpper();
            }

            if (ModelState.IsValid)
            {
                _context.QrPasses.Add(qrPass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            LoadRegistrationDropdown(qrPass.RegistrationId);
            return View(qrPass);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var qrPass = await _context.QrPasses.FindAsync(id);
            if (qrPass == null) return NotFound();

            LoadRegistrationDropdown(qrPass.RegistrationId);
            return View(qrPass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QrPass qrPass)
        {
            if (id != qrPass.QrPassId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(qrPass);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.QrPasses.Any(e => e.QrPassId == qrPass.QrPassId))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            LoadRegistrationDropdown(qrPass.RegistrationId);
            return View(qrPass);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var qrPass = await _context.QrPasses
                .Include(q => q.Registration)
                .ThenInclude(r => r.Participant)
                .Include(q => q.Registration)
                .ThenInclude(r => r.Event)
                .FirstOrDefaultAsync(q => q.QrPassId == id);

            if (qrPass == null) return NotFound();

            return View(qrPass);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var qrPass = await _context.QrPasses.FindAsync(id);
            if (qrPass != null)
            {
                _context.QrPasses.Remove(qrPass);
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
                    DisplayText = $"Reg #{r.RegistrationId} - {r.Participant?.StudentId ?? "No Student"} - {r.Event?.EventTitle ?? "No Event"}"
                })
                .ToList();

            ViewBag.RegistrationId = new SelectList(registrations, "RegistrationId", "DisplayText", selectedRegistration);
        }
    }
}