using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.ViewModels;
using ZKAttendanceWeb.Services.Employees;
using ZKAttendanceWeb.Services.Common;

namespace ZKAttendanceWeb.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly LookupService _lookupService;
        private readonly ZKAttendanceWebDbContext _context;
        private readonly ILogger<EmployeesController> _logger;
        public EmployeesController(
    IEmployeeService employeeService,
    LookupService lookupService,
    ZKAttendanceWebDbContext context,
    ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _lookupService = lookupService;
            _context = context;
            _logger = logger;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                return View(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض قائمة الموظفين");
                TempData["Error"] = "حدث خطأ أثناء تحميل قائمة الموظفين";
                return View(new List<Employee>());
            }
        }

        // GET: Employees/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var nextBiometricId = await _employeeService.GetNextBiometricUserIdAsync();
                var lastBiometricIds = await _employeeService.GetLastBiometricUserIdsAsync(10);

                var viewModel = new EmployeeFormViewModel
                {
                    BiometricUserId = nextBiometricId,
                    IsActive = true,
                    CheckAttendance = true,
                    CheckLate = true,
                    CheckEarly = true,
                    CheckOvertime = true,
                    CheckHoliday = true
                };

                ViewBag.NextBiometricId = nextBiometricId;
                ViewBag.LastBiometricIds = lastBiometricIds;

                await PopulateDropdowns(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فتح صفحة إضافة موظف");
                TempData["Error"] = "حدث خطأ أثناء فتح الصفحة";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeFormViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ 1. إنشاء الموظف
                    var employee = new Employee
                    {
                        BiometricUserId = viewModel.BiometricUserId,
                        EmployeeName = viewModel.EmployeeName,
                        SSN = viewModel.SSN,
                        Gender = viewModel.Gender,
                        Title = viewModel.Title,
                        PhoneNumber = viewModel.PhoneNumber,
                        BirthDate = viewModel.BirthDate,
                        HireDate = viewModel.HireDate,
                        DepartmentId = viewModel.DepartmentId,
                        DefaultShiftId = viewModel.DefaultShiftId,
                        CheckAttendance = viewModel.CheckAttendance,
                        CheckLate = viewModel.CheckLate,
                        CheckEarly = viewModel.CheckEarly,
                        CheckOvertime = viewModel.CheckOvertime,
                        CheckHoliday = viewModel.CheckHoliday,
                        IsActive = viewModel.IsActive
                    };

                    await _employeeService.CreateEmployeeAsync(employee);

                    // ✅ 2. ربط الموظف بالأجهزة المختارة
                    await SaveEmployeeDevices(employee.EmployeeId, viewModel.SelectedDeviceIds);

                    // ✅ 3. ربط الموظف بالفروع (إذا موجودة)
                    if (viewModel.SelectedBranchIds?.Any() == true)
                    {
                        await SaveEmployeeBranches(employee.EmployeeId, viewModel.SelectedBranchIds);
                    }

                    TempData["Success"] = "تم إنشاء الموظف بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("BiometricUserId", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء موظف");
                    ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الموظف");
                }
            }

            // ✅ إعادة تحميل ViewBag للبصمة عند الخطأ
            ViewBag.NextBiometricId = viewModel.BiometricUserId;
            ViewBag.LastBiometricIds = await _employeeService.GetLastBiometricUserIdsAsync(10);

            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // ✅ استعلام مبسط وآمن
                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "الموظف غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ جلب البيانات المرتبطة بشكل منفصل (آمن من null)
                var selectedBranches = await _context.EmployeeBranches
                    .Where(eb => eb.EmployeeId == id && eb.IsActive)
                    .Select(eb => eb.BranchId)
                    .ToListAsync();

                var selectedDevices = await _context.EmployeeDevices
                    .Where(ed => ed.EmployeeId == id && ed.IsActive)
                    .Select(ed => ed.DeviceId)
                    .ToListAsync();

                var viewModel = new EmployeeFormViewModel
                {
                    EmployeeId = employee.EmployeeId,
                    BiometricUserId = employee.BiometricUserId,
                    EmployeeName = employee.EmployeeName,
                    SSN = employee.SSN,
                    Gender = employee.Gender,
                    Title = employee.Title,
                    PhoneNumber = employee.PhoneNumber,
                    BirthDate = employee.BirthDate,
                    HireDate = employee.HireDate,
                    DepartmentId = employee.DepartmentId,
                    DefaultShiftId = employee.DefaultShiftId,
                    CheckAttendance = employee.CheckAttendance,
                    CheckLate = employee.CheckLate,
                    CheckEarly = employee.CheckEarly,
                    CheckOvertime = employee.CheckOvertime,
                    CheckHoliday = employee.CheckHoliday,
                    IsActive = employee.IsActive,
                    SelectedBranchIds = selectedBranches,
                    SelectedDeviceIds = selectedDevices
                };

                await PopulateDropdowns(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الموظف {EmployeeId}. الخطأ: {Message}", id, ex.Message);
                TempData["Error"] = $"حدث خطأ أثناء تحميل بيانات الموظف: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeFormViewModel viewModel)
        {
            if (id != viewModel.EmployeeId)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == id);

                    if (employee == null)
                    {
                        TempData["Error"] = "الموظف غير موجود";
                        return RedirectToAction(nameof(Index));
                    }

                    // ✅ تحديث بيانات الموظف
                    employee.BiometricUserId = viewModel.BiometricUserId;
                    employee.EmployeeName = viewModel.EmployeeName;
                    employee.SSN = viewModel.SSN;
                    employee.Gender = viewModel.Gender;
                    employee.Title = viewModel.Title;
                    employee.PhoneNumber = viewModel.PhoneNumber;
                    employee.BirthDate = viewModel.BirthDate;
                    employee.HireDate = viewModel.HireDate;
                    employee.DepartmentId = viewModel.DepartmentId;
                    employee.DefaultShiftId = viewModel.DefaultShiftId;
                    employee.CheckAttendance = viewModel.CheckAttendance;
                    employee.CheckLate = viewModel.CheckLate;
                    employee.CheckEarly = viewModel.CheckEarly;
                    employee.CheckOvertime = viewModel.CheckOvertime;
                    employee.CheckHoliday = viewModel.CheckHoliday;
                    employee.IsActive = viewModel.IsActive;
                    employee.ModifiedDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // ✅ تحديث الأجهزة
                    await SaveEmployeeDevices(employee.EmployeeId, viewModel.SelectedDeviceIds);

                    // ✅ تحديث الفروع (إذا موجودة)
                    if (viewModel.SelectedBranchIds?.Any() == true)
                    {
                        await SaveEmployeeBranches(employee.EmployeeId, viewModel.SelectedBranchIds);
                    }

                    TempData["Success"] = "تم تحديث الموظف بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("BiometricUserId", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحديث الموظف {EmployeeId}", id);
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث الموظف");
                }
            }

            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.DefaultShift)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                {
                    TempData["Error"] = "الموظف غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ جلب الأجهزة والفروع بشكل منفصل (آمن من null)
                var assignedDevices = await _context.EmployeeDevices
                    .Where(ed => ed.EmployeeId == id && ed.IsActive)
                    .Include(ed => ed.Device)
                        .ThenInclude(d => d!.Branch)
                    .Where(ed => ed.Device != null && ed.Device.IsActive && ed.Device.Branch != null)
                    .ToListAsync();

                var viewModel = new EmployeeDetailsViewModel
                {
                    Employee = employee,
                    AssignedBranches = assignedDevices
                        .GroupBy(ed => ed.Device!.Branch!)
                        .Select(g => new EmployeeDetailsViewModel.BranchWithDevicesViewModel
                        {
                            BranchId = g.Key.BranchId,
                            BranchCode = g.Key.BranchCode,
                            BranchName = g.Key.BranchName,
                            City = g.Key.City,
                            Devices = g.Select(ed => new EmployeeDetailsViewModel.DeviceInfoViewModel
                            {
                                DeviceId = ed.Device!.DeviceId,
                                DeviceName = ed.Device.DeviceName,
                                DeviceIP = ed.Device.DeviceIP,
                                SerialNumber = ed.Device.SerialNumber
                            }).ToList()
                        })
                        .OrderBy(b => b.City)
                        .ThenBy(b => b.BranchName)
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تفاصيل الموظف {EmployeeId}. الخطأ الكامل: {Message}\n{StackTrace}",
                    id, ex.Message, ex.StackTrace);
                TempData["Error"] = $"حدث خطأ أثناء تحميل التفاصيل: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Employees/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _employeeService.DeleteEmployeeAsync(id);
                TempData["Success"] = "تم حذف الموظف بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الموظف {EmployeeId}", id);
                TempData["Error"] = "حدث خطأ أثناء حذف الموظف";
            }
            return RedirectToAction(nameof(Index));
        }

        // ═════════════════════════════════════════════════════════════
        // ✅ Helper Methods
        // ═════════════════════════════════════════════════════════════

        /// <summary>
        /// حفظ/تحديث ربط الموظف بالأجهزة المحددة
        /// </summary>
        private async Task SaveEmployeeDevices(int employeeId, List<int>? selectedDeviceIds)
        {
            // ✅ حذف الأجهزة القديمة
            var existingDevices = await _context.EmployeeDevices
                .Where(ed => ed.EmployeeId == employeeId)
                .ToListAsync();

            _context.EmployeeDevices.RemoveRange(existingDevices);

            // ✅ إضافة الأجهزة الجديدة
            if (selectedDeviceIds?.Any() == true)
            {
                foreach (var deviceId in selectedDeviceIds)
                {
                    _context.EmployeeDevices.Add(new EmployeeDevice
                    {
                        EmployeeId = employeeId,
                        DeviceId = deviceId,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// حفظ/تحديث ربط الموظف بالفروع
        /// </summary>
        private async Task SaveEmployeeBranches(int employeeId, List<int>? selectedBranchIds)
        {
            // ✅ حذف الفروع القديمة
            var existingBranches = await _context.EmployeeBranches
                .Where(eb => eb.EmployeeId == employeeId)
                .ToListAsync();

            _context.EmployeeBranches.RemoveRange(existingBranches);

            // ✅ إضافة الفروع الجديدة
            if (selectedBranchIds?.Any() == true)
            {
                foreach (var branchId in selectedBranchIds)
                {
                    _context.EmployeeBranches.Add(new EmployeeBranch
                    {
                        EmployeeId = employeeId,
                        BranchId = branchId,
                        AssignedDate = DateTime.Now,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// ملء Dropdowns + قائمة الفروع والأجهزة
        /// </summary>
        private async Task PopulateDropdowns(EmployeeFormViewModel viewModel)
        {
            // ✅ Departments & Shifts
            ViewBag.Departments = new SelectList(
                await _lookupService.GetActiveDepartmentsAsync(),
                "DepartmentId",
                "DepartmentName",
                viewModel.DepartmentId
            );

            ViewBag.Shifts = new SelectList(
                await _lookupService.GetActiveShiftsAsync(),
                "ShiftId",
                "ShiftName",
                viewModel.DefaultShiftId
            );

            // ✅ الفروع والأجهزة مجموعة حسب المدينة
            var branches = await _context.Branches
                .Include(b => b.Devices.Where(d => d.IsActive))
                .Where(b => b.IsActive)
                .OrderBy(b => b.City)
                .ThenBy(b => b.BranchName)
                .ToListAsync();

            viewModel.BranchesByCity = branches
                .GroupBy(b => b.City ?? "غير محدد")
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(b => new BranchDeviceViewModel
                    {
                        BranchId = b.BranchId,
                        BranchCode = b.BranchCode,
                        BranchName = b.BranchName,
                        City = b.City,
                        IsBranchSelected = viewModel.SelectedBranchIds?.Contains(b.BranchId) ?? false,
                        Devices = b.Devices.Select(d => new DeviceSelectionViewModel
                        {
                            DeviceId = d.DeviceId,
                            DeviceName = d.DeviceName,
                            DeviceIP = d.DeviceIP,
                            SerialNumber = d.SerialNumber,
                            IsSelected = viewModel.SelectedDeviceIds?.Contains(d.DeviceId) ?? false
                        }).ToList()
                    }).ToList()
                );
        }

        // GET: Employees/UnregisteredBiometricIds
        public async Task<IActionResult> UnregisteredBiometricIds()
        {
            try
            {
                var unregisteredIds = await _employeeService.GetUnregisteredBiometricIdsAsync();

                ViewBag.Message = unregisteredIds.Any()
                    ? $"تم العثور على {unregisteredIds.Count} رقم بصمة غير مسجل"
                    : "جميع أرقام البصمة مسجلة";

                return View(unregisteredIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض أرقام البصمة غير المسجلة");
                TempData["Error"] = "حدث خطأ أثناء تحميل القائمة";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Employees/CreateFromBiometricId
        public async Task<IActionResult> CreateFromBiometricId(string biometricUserId)
        {
            if (string.IsNullOrEmpty(biometricUserId))
            {
                TempData["Error"] = "رقم البصمة غير صحيح";
                return RedirectToAction(nameof(UnregisteredBiometricIds));
            }

            try
            {
                var nextBiometricId = await _employeeService.GetNextBiometricUserIdAsync();
                var lastBiometricIds = await _employeeService.GetLastBiometricUserIdsAsync(10);

                var viewModel = new EmployeeFormViewModel
                {
                    BiometricUserId = biometricUserId,
                    IsActive = true,
                    CheckAttendance = true,
                    CheckLate = true,
                    CheckEarly = true,
                    CheckOvertime = true,
                    CheckHoliday = true
                };

                ViewBag.NextBiometricId = nextBiometricId;
                ViewBag.LastBiometricIds = lastBiometricIds;
                ViewBag.FromUnregistered = true;

                await PopulateDropdowns(viewModel);
                return View("Create", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فتح صفحة إضافة موظف من رقم البصمة {BiometricUserId}", biometricUserId);
                TempData["Error"] = "حدث خطأ أثناء فتح الصفحة";
                return RedirectToAction(nameof(UnregisteredBiometricIds));
            }
        }
    }
}
    