using Convocation.DataAccess;
using Convocation.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Convocation_Management_System.Web.UI.Controllers
{
    public class FoodMenuController : Controller
    {
        private readonly ConvocationDbContext _context;

        public FoodMenuController(ConvocationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.FoodMenu
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync());
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FoodMenu model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.FoodMenu.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Food menu created successfully.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _context.FoodMenu.FindAsync(id);

            if (menu == null)
                return NotFound();

            return View(menu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FoodMenu model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.FoodMenu.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Food menu updated.";

            return RedirectToAction(nameof(Index));
        }
    }
}