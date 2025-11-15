using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Services.Departments;

namespace ZKAttendanceWeb.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(
            IDepartmentService departmentService,
            ILogger<DepartmentsController> logger)
        {
            _departmentService = departmentService;
            _logger = logger;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            try
            {
                var departments = await _departmentService.GetAllDepartmentsAsync();
                return View(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض قائمة الأقسام");
                TempData["Error"] = "حدث خطأ أثناء تحميل قائمة الأقسام";
                return View(new List<Department>());
            }
        }

        // GET: Departments/Create
        public async Task<IActionResult> Create()
        {
            await PopulateParentDepartments();
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _departmentService.CreateDepartmentAsync(department);
                    TempData["Success"] = "تم إنشاء القسم بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء قسم");
                    ModelState.AddModelError("", "حدث خطأ أثناء إنشاء القسم");
                }
            }

            await PopulateParentDepartments(department.ParentDepartmentId);
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var department = await _departmentService.GetDepartmentByIdAsync(id);
                if (department == null)
                {
                    TempData["Error"] = "القسم غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateParentDepartments(department.ParentDepartmentId, id);
                return View(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات القسم {DepartmentId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل بيانات القسم";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _departmentService.UpdateDepartmentAsync(department);
                    TempData["Success"] = "تم تحديث القسم بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحديث القسم {DepartmentId}", id);
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث القسم");
                }
            }

            await PopulateParentDepartments(department.ParentDepartmentId, id);
            return View(department);
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var department = await _departmentService.GetDepartmentByIdAsync(id);
                if (department == null)
                {
                    TempData["Error"] = "القسم غير موجود";
                    return RedirectToAction(nameof(Index));
                }
                return View(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تفاصيل القسم {DepartmentId}", id);
                TempData["Error"] = "حدث خطأ أثناء عرض تفاصيل القسم";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Departments/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _departmentService.DeleteDepartmentAsync(id);
                TempData["Success"] = "تم حذف القسم بنجاح";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف القسم {DepartmentId}", id);
                TempData["Error"] = "حدث خطأ أثناء حذف القسم";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateParentDepartments(int? selectedId = null, int? excludeId = null)
        {
            var departments = await _departmentService.GetAllDepartmentsAsync();

            if (excludeId.HasValue)
                departments = departments.Where(d => d.DepartmentId != excludeId.Value).ToList();

            ViewBag.ParentDepartments = new SelectList(departments, "DepartmentId", "DepartmentName", selectedId);
        }
    }
}
