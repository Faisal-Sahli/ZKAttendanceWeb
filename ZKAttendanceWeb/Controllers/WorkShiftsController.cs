using Microsoft.AspNetCore.Mvc;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Services.WorkShifts;

namespace ZKAttendanceWeb.Controllers
{
    public class WorkShiftsController : Controller
    {
        private readonly IWorkShiftService _shiftService;
        private readonly ILogger<WorkShiftsController> _logger;

        public WorkShiftsController(
            IWorkShiftService shiftService,
            ILogger<WorkShiftsController> logger)
        {
            _shiftService = shiftService;
            _logger = logger;
        }

        // GET: WorkShifts
        public async Task<IActionResult> Index()
        {
            try
            {
                var shifts = await _shiftService.GetAllShiftsAsync();
                return View(shifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض قائمة الشفتات");
                TempData["Error"] = "حدث خطأ أثناء تحميل قائمة الشفتات";
                return View(new List<WorkShift>());
            }
        }

        // GET: WorkShifts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: WorkShifts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkShift shift)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _shiftService.CreateShiftAsync(shift);
                    TempData["Success"] = "تم إنشاء الشفت بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء شفت");
                    ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الشفت");
                }
            }
            return View(shift);
        }

        // GET: WorkShifts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var shift = await _shiftService.GetShiftByIdAsync(id);
                if (shift == null)
                {
                    TempData["Error"] = "الشفت غير موجود";
                    return RedirectToAction(nameof(Index));
                }
                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الشفت {ShiftId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل بيانات الشفت";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkShifts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkShift shift)
        {
            if (id != shift.ShiftId)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _shiftService.UpdateShiftAsync(shift);
                    TempData["Success"] = "تم تحديث الشفت بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحديث الشفت {ShiftId}", id);
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث الشفت");
                }
            }
            return View(shift);
        }

        // GET: WorkShifts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var shift = await _shiftService.GetShiftByIdAsync(id);
                if (shift == null)
                {
                    TempData["Error"] = "الشفت غير موجود";
                    return RedirectToAction(nameof(Index));
                }
                return View(shift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تفاصيل الشفت {ShiftId}", id);
                TempData["Error"] = "حدث خطأ أثناء عرض تفاصيل الشفت";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: WorkShifts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _shiftService.DeleteShiftAsync(id);
                TempData["Success"] = "تم حذف الشفت بنجاح";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الشفت {ShiftId}", id);
                TempData["Error"] = "حدث خطأ أثناء حذف الشفت";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
