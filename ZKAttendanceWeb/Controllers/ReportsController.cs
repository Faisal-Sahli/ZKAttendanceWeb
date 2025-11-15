using Microsoft.AspNetCore.Mvc;
using ZKAttendanceWeb.DTOs.Reports;
using ZKAttendanceWeb.Services.Attendances;
using ZKAttendanceWeb.Services.Common;
using ZKAttendanceWeb.Services.Report;
using ZKAttendanceWeb.ViewModels;

namespace ZKAttendanceWeb.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IAttendanceService _attendanceService;  
        private readonly LookupService _lookupService;
        private readonly ILogger<ReportsController> _logger;     

        public ReportsController(
            IReportService reportService,
            IAttendanceService attendanceService,  // ✅ جديد
            LookupService lookupService,
            ILogger<ReportsController> logger)     // ✅ جديد
        {
            _reportService = reportService;
            _attendanceService = attendanceService;
            _lookupService = lookupService;
            _logger = logger;
        }

        // GET: Reports/Index
        public IActionResult Index()
        {
            return View();
        }

        // ═════════════════════════════════════════════════════════════
        // DailyReportFilter - التقرير اليومي مع الفلاتر المتقدمة
        // ═════════════════════════════════════════════════════════════

        // GET: Reports/DailyReportFilter
        public async Task<IActionResult> DailyReportFilter()
        {
            try
            {
                var model = new DailyReportFilterViewModel
                {
                    Branches = await _lookupService.GetActiveBranchesAsync(),
                    Devices = await _lookupService.GetActiveDevicesAsync(),
                    Employees = await _lookupService.GetActiveEmployeesAsync(),
                    DateFrom = DateTime.Today.AddDays(-7),
                    DateTo = DateTime.Today
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة الفلتر");
                TempData["Error"] = "حدث خطأ أثناء تحميل الصفحة";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Reports/DailyReportFilter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DailyReportFilter(DailyReportFilterViewModel filter)
        {
            if (!ModelState.IsValid)
            {
                await PopulateFilterLists(filter);
                return View(filter);
            }

            try
            {
                var report = await _reportService.GetDailyAttendanceRangeReportAsync(
                    filter.DateFrom,
                    filter.DateTo,
                    filter.SelectedBranchId,
                    filter.SelectedDeviceId,
                    filter.SelectedEmployeeId);

                filter.Report = report;
                await PopulateFilterLists(filter);

                return View(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في توليد التقرير");
                ModelState.AddModelError("", $"حدث خطأ أثناء توليد التقرير: {ex.Message}");
                await PopulateFilterLists(filter);
                return View(filter);
            }
        }

        // ═════════════════════════════════════════════════════════════
        // DailyAttendance - تقرير الحضور اليومي الشامل
        // ═════════════════════════════════════════════════════════════

        // GET: Reports/DailyAttendance
        public async Task<IActionResult> DailyAttendance(
            DateTime? date,
            int? branchId,
            int? departmentId)
        {
            try
            {
                var selectedDate = date ?? DateTime.Today;

                // جلب التقرير الشامل مع قوائم الحاضرين والغائبين
                var summary = await _attendanceService.GetDailyAttendanceReportSummaryAsync(
                    selectedDate,
                    branchId,
                    departmentId);

                // تعبئة ViewBag للفلاتر
                ViewBag.SelectedDate = selectedDate;
                ViewBag.BranchId = branchId;
                ViewBag.DepartmentId = departmentId;
                ViewBag.Branches = await _lookupService.GetActiveBranchesAsync();
                ViewBag.Departments = await _lookupService.GetActiveDepartmentsAsync();

                return View(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تقرير الحضور اليومي");
                TempData["Error"] = "حدث خطأ أثناء تحميل التقرير";
                return View(new DailyAttendanceReportSummaryDto());
            }
        }

        // ═════════════════════════════════════════════════════════════
        // MonthlyAttendance - تقرير الحضور الشهري
        // ═════════════════════════════════════════════════════════════

        // GET: Reports/MonthlyAttendance
        public async Task<IActionResult> MonthlyAttendance(
            int? year,
            int? month,
            int? branchId)
        {
            try
            {
                var selectedYear = year ?? DateTime.Today.Year;
                var selectedMonth = month ?? DateTime.Today.Month;

                ViewBag.SelectedYear = selectedYear;
                ViewBag.SelectedMonth = selectedMonth;
                ViewBag.BranchId = branchId;
                ViewBag.Branches = await _lookupService.GetActiveBranchesAsync();

                // يمكنك لاحقاً إضافة Service للتقرير الشهري
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تقرير الحضور الشهري");
                TempData["Error"] = "حدث خطأ أثناء تحميل التقرير";
                return View();
            }
        }

        // ═════════════════════════════════════════════════════════════
        // EmployeeAttendance - تقرير حضور موظف معين
        // ═════════════════════════════════════════════════════════════

        // GET: Reports/EmployeeAttendance
        public async Task<IActionResult> EmployeeAttendance(
            int? employeeId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            try
            {
                ViewBag.EmployeeId = employeeId;
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                ViewBag.Employees = await _lookupService.GetActiveEmployeesAsync();

                // يمكنك لاحقاً إضافة Service لتقرير الموظف
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تقرير حضور الموظف");
                TempData["Error"] = "حدث خطأ أثناء تحميل التقرير";
                return View();
            }
        }

        // ═════════════════════════════════════════════════════════════
        // Export Actions - تصدير التقارير
        // ═════════════════════════════════════════════════════════════

        // GET: Reports/ExportDailyToExcel
        public async Task<IActionResult> ExportDailyToExcel(
            DateTime? date,
            int? branchId,
            int? departmentId)
        {
            try
            {
                var selectedDate = date ?? DateTime.Today;
                var summary = await _attendanceService.GetDailyAttendanceReportSummaryAsync(
                    selectedDate,
                    branchId,
                    departmentId);

                // يمكنك استخدام ExcelReportService لتصدير Excel
                // var excelFile = await _reportService.ExportDailyAttendanceToExcelAsync(summary);
                // return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                //             $"DailyAttendance_{selectedDate:yyyy-MM-dd}.xlsx");

                TempData["Info"] = "ميزة التصدير قيد التطوير";
                return RedirectToAction(nameof(DailyAttendance), new { date, branchId, departmentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تصدير التقرير إلى Excel");
                TempData["Error"] = "حدث خطأ أثناء التصدير";
                return RedirectToAction(nameof(DailyAttendance));
            }
        }

        // GET: Reports/ExportDailyToPdf
        public async Task<IActionResult> ExportDailyToPdf(
            DateTime? date,
            int? branchId,
            int? departmentId)
        {
            try
            {
                var selectedDate = date ?? DateTime.Today;
                var summary = await _attendanceService.GetDailyAttendanceReportSummaryAsync(
                    selectedDate,
                    branchId,
                    departmentId);

                // يمكنك استخدام PdfReportService لتصدير PDF
                // var pdfFile = await _reportService.ExportDailyAttendanceToPdfAsync(summary);
                // return File(pdfFile, "application/pdf", 
                //             $"DailyAttendance_{selectedDate:yyyy-MM-dd}.pdf");

                TempData["Info"] = "ميزة التصدير قيد التطوير";
                return RedirectToAction(nameof(DailyAttendance), new { date, branchId, departmentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تصدير التقرير إلى PDF");
                TempData["Error"] = "حدث خطأ أثناء التصدير";
                return RedirectToAction(nameof(DailyAttendance));
            }
        }

        // ═════════════════════════════════════════════════════════════
        // Helper Methods
        // ═════════════════════════════════════════════════════════════

        private async Task PopulateFilterLists(DailyReportFilterViewModel filter)
        {
            filter.Branches = await _lookupService.GetActiveBranchesAsync();
            filter.Devices = await _lookupService.GetActiveDevicesAsync();
            filter.Employees = await _lookupService.GetActiveEmployeesAsync();
        }
    }
}
