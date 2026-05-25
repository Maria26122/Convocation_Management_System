using Convocation.DataAccess;
using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
   
    public class DistributionTaskController : Controller
    {
        private readonly ConvocationDbContext _context;

        public DistributionTaskController(ConvocationDbContext context)
        {
            _context = context;
        }

        // =========================
        // TASK LIST
        // =========================

        public async Task<IActionResult> Index()
        {
            var tasks = await _context.DistributionTask
                .Include(t => t.Event)
                .Include(t => t.AssignedStaff)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        // =========================
        // CREATE
        // =========================

        public IActionResult Create()
        {
            var model = new StaffTaskViewModel();

            model.DistributionTasks = _context.DistributionTask
                .Select(t => new SelectListItem
                {
                    Value = t.DistributionTaskId.ToString(),
                    Text = t.TaskTitle
                }).ToList();

            model.staffs = _context.UserAccount
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Staff")
                .Select(u => new SelectListItem
                {
                    Value = u.UserAccountId.ToString(),
                    Text = u.FullName
                }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DistributionTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            var task = new DistributionTask
            {
                EventId = model.EventId,
                TaskTitle = model.TaskTitle,
                Description = model.Description,
                DistributionType = model.DistributionType,
                AssignedStaffId = model.AssignedStaffId,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.DistributionTask.Add(task);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Distribution task created.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT
        // =========================

        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.DistributionTask.FindAsync(id);

            if (task == null)
                return NotFound();

            var model = new DistributionTaskViewModel
            {
                DistributionTaskId = task.DistributionTaskId,
                EventId = task.EventId,
                TaskTitle = task.TaskTitle,
                Description = task.Description,
                DistributionType = task.DistributionType,
                AssignedStaffId = task.AssignedStaffId,
                Status = task.Status,
                Remarks = task.Remarks
            };

            LoadDropdowns(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DistributionTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns(model);
                return View(model);
            }

            var task = await _context.DistributionTask
                .FirstOrDefaultAsync(x => x.DistributionTaskId == model.DistributionTaskId);

            if (task == null)
                return NotFound();

            task.EventId = model.EventId;
            task.TaskTitle = model.TaskTitle;
            task.Description = model.Description;
            task.DistributionType = model.DistributionType;
            task.AssignedStaffId = model.AssignedStaffId;
            task.Status = model.Status;
            task.Remarks = model.Remarks;

            if (model.Status == "Completed" && task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync(); // ❗ REMOVE Update()

            TempData["Success"] = "Task updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================

        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.DistributionTask
                .Include(t => t.Event)
                .Include(t => t.AssignedStaff)
                .FirstOrDefaultAsync(t => t.DistributionTaskId == id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.DistributionTask.FindAsync(id);

            if (task != null)
            {
                _context.DistributionTask.Remove(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // STAFF TASKS
        // =========================

        public async Task<IActionResult> MyTasks()
        {
            var userId =
                Convert.ToInt32(HttpContext.Session.GetString("UserId"));

            var tasks = await _context.DistributionTask
                .Include(t => t.Event)
                .Where(t => t.AssignedStaffId == userId)
                .ToListAsync();

            return View(tasks);
        }

        public async Task<IActionResult> ScanQr(int id)
        {
            var task = await _context.DistributionTask
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.DistributionTaskId == id);

            if (task == null)
                return NotFound();


            var model = new QrDistributionViewModel
            {
                DistributionTaskId = task.DistributionTaskId,
                DistributionType = task.DistributionType
            };

            ViewBag.Task = task;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ScanQr(QrDistributionViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(userIdString);

            var task = await _context.DistributionTask
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.DistributionTaskId == model.DistributionTaskId);

            if (task == null)
                return NotFound();

            ViewBag.Task = task;

            // =========================
            // TASK STATUS CHECK
            // =========================
            if (task.Status != "In Progress")
            {
                model.IsSuccess = false;
                model.Message = "Task is not active.";
                return View(model);
            }

            // =========================
            // FIND QR
            // =========================
            var qr = await _context.QrPass
                .Include(q => q.Registration)
                    .ThenInclude(r => r.Participant)
                        .ThenInclude(p => p.UserAccount)
                .FirstOrDefaultAsync(q => q.QrCodeText == model.QrCodeText);

            if (qr == null)
            {
                model.IsSuccess = false;
                model.Message = "Invalid QR Code";
                return View(model);
            }

            // =========================
            // CHECK PAYMENT
            // =========================
            var payment = await _context.Payment
                .FirstOrDefaultAsync(p =>
                    p.RegistrationId == qr.RegistrationId &&
                    p.PaymentStatus == "Paid");

            if (payment == null)
            {
                model.IsSuccess = false;
                model.Message = "Payment not completed.";
                return View(model);
            }

            // =========================
            // CHECK DUPLICATE
            // =========================
            var alreadyDistributed = await _context.DistributionLog
                .AnyAsync(d =>
                    d.RegistrationId == qr.RegistrationId &&
                    d.ActionType == task.DistributionType);

            if (alreadyDistributed)
            {
                model.IsSuccess = false;
                model.Message = $"{task.DistributionType} already distributed.";
                return View(model);
            }

            // =========================
            // CREATE LOG
            // =========================
            var log = new DistributionLog
            {
                RegistrationId = qr.RegistrationId,
                ParticipantId = qr.Registration.ParticipantId,
                EventId = qr.Registration.EventId,
                UserAccountId = userId,

                DistributionTaskId = task.DistributionTaskId,
                ActionType = task.DistributionType,

                ActionDate = DateTime.Now,
                Note = $"{task.DistributionType} distributed successfully.",
                Remarks = "QR Verified",

                IsDelivered = true,
                IsQrVerified = true
            };

            _context.DistributionLog.Add(log);


            // AUTO COMPLETE TASK

            var totalRegistrations = await _context.Registration
                .CountAsync(r => r.EventId == task.EventId);

            var completedCount = await _context.DistributionLog
                .CountAsync(d =>
                    d.EventId == task.EventId &&
                    d.ActionType == task.DistributionType);

            if (completedCount >= totalRegistrations)
            {
                task.Status = "Completed";
                task.CompletedAt = DateTime.Now;
            }

            // =========================
            // LOG SUCCESS ACTIVITY ONLY
            // =========================
            _context.OperationActivityLog.Add(new OperationActivityLog
            {
                ActivityType = "QR_SCAN",
                Message = $"{qr.Registration.Participant.UserAccount.FullName} received {task.DistributionType}",
                UserAccountId = userId,
                Time = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // =========================
            // RESPONSE
            // =========================
            model.IsSuccess = true;
            model.StudentName = qr.Registration.Participant.UserAccount.FullName;
            model.StudentId = qr.Registration.Participant.StudentId;
            model.Message = $"{task.DistributionType} distributed successfully.";

            return View(model);
        }



        // =========================
        // LOAD DROPDOWNS
        // =========================

        private void LoadDropdowns(DistributionTaskViewModel model)
        {
            model.Events = _context.Event
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = e.EventTitle
                }).ToList();

            model.Staffs = _context.UserAccount
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Staff")
                .Select(u => new SelectListItem
                {
                    Value = u.UserAccountId.ToString(),
                    Text = u.FullName
                }).ToList();
        }
    }
}