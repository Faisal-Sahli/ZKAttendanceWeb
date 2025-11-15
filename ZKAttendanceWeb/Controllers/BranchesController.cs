using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Services.Branches;

namespace ZKAttendanceWeb.Controllers
{
    public class BranchesController : Controller
    {
        private readonly IBrancheService _branchService;
        private readonly ZKAttendanceWebDbContext _context;
        private readonly ILogger<BranchesController> _logger;

        public BranchesController(
            IBrancheService branchService,
            ZKAttendanceWebDbContext context,
            ILogger<BranchesController> logger)
        {
            _branchService = branchService;
            _context = context;
            _logger = logger;
        }

        // GET: Branches
        public async Task<IActionResult> Index()
        {
            try
            {
                var branches = await _context.Branches
                    .Include(b => b.Devices.Where(d => d.IsActive))
                    .Include(b => b.EmployeeBranches.Where(eb => eb.IsActive))
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.City)
                    .ThenBy(b => b.BranchName)
                    .AsNoTracking()
                    .ToListAsync();

                return View(branches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض قائمة الفروع");
                TempData["Error"] = "حدث خطأ أثناء تحميل قائمة الفروع";
                return View(new List<Branch>());
            }
        }

        // GET: Branches/Create
        public IActionResult Create()
        {
            var branch = new Branch
            {
                IsActive = true
            };
            return View(branch);
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch branch)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _branchService.CreateBranchAsync(branch);
                    TempData["Success"] = "تم إنشاء الفرع بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("BranchCode", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء فرع جديد");
                    ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الفرع");
                }
            }
            return View(branch);
        }

        // GET: Branches/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var branch = await _branchService.GetBranchByIdAsync(id);
                if (branch == null)
                {
                    TempData["Error"] = "الفرع غير موجود";
                    return RedirectToAction(nameof(Index));
                }
                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الفرع {BranchId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل بيانات الفرع";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Branch branch)
        {
            if (id != branch.BranchId)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _branchService.UpdateBranchAsync(branch);
                    TempData["Success"] = "تم تحديث الفرع بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("BranchCode", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحديث الفرع {BranchId}", id);
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث الفرع");
                }
            }
            return View(branch);
        }

        // GET: Branches/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var branch = await _context.Branches
                    .Include(b => b.Devices.Where(d => d.IsActive))
                    .Include(b => b.EmployeeBranches.Where(eb => eb.IsActive))
                        .ThenInclude(eb => eb.Employee)
                            .ThenInclude(e => e.Department)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BranchId == id);

                if (branch == null)
                {
                    TempData["Error"] = "الفرع غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تفاصيل الفرع {BranchId}", id);
                TempData["Error"] = "حدث خطأ أثناء عرض تفاصيل الفرع";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Branches/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _branchService.DeleteBranchAsync(id);
                TempData["Success"] = "تم حذف الفرع بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الفرع {BranchId}", id);
                TempData["Error"] = "حدث خطأ أثناء حذف الفرع";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
