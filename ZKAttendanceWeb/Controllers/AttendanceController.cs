using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Repostries;
using ZKAttendanceWeb.Services.Attendances;
using ZKAttendanceWeb.Services.Common;
using ZKAttendanceWeb.Services.Report;
using ZKAttendanceWeb.ViewModels;

namespace ZKAttendanceWeb.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ZKAttendanceWebDbContext _context;
        private readonly ILogger<AttendanceController> _logger;
        private readonly AttendanceQueryService _queryService;
        private readonly AttendanceCalculationService _calculationService;
        private readonly ExcelReportService _excelService;
        private readonly PdfReportService _pdfService;
        private readonly LookupService _lookupService;

        public AttendanceController(
            ZKAttendanceWebDbContext context,
            ILogger<AttendanceController> logger,
            AttendanceQueryService queryService,
            AttendanceCalculationService calculationService,
            ExcelReportService excelService,
            PdfReportService pdfService,
            LookupService lookupService)
        {
            _context = context;
            _logger = logger;
            _queryService = queryService;
            _calculationService = calculationService;
            _excelService = excelService;
            _pdfService = pdfService;
            _lookupService = lookupService;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // GET: Attendance/Index
        public async Task<IActionResult> Index(
            string? searchString,
            DateTime? fromDate,
            DateTime? toDate,
            int? branchId,
            int? deviceId,
            string? attendanceStatus,
            int? minWorkHours,
            int? maxWorkHours,
            string? quickFilter,
            int pageNumber = 1)
        {
            int pageSize = 50;
            ApplyQuickFilter(quickFilter, ref fromDate, ref toDate);

            bool hasFilters = !string.IsNullOrEmpty(searchString) ||
                              fromDate.HasValue ||
                              toDate.HasValue ||
                              branchId.HasValue ||
                              deviceId.HasValue ||
                              !string.IsNullOrEmpty(attendanceStatus) ||
                              minWorkHours.HasValue ||
                              maxWorkHours.HasValue ||
                              !string.IsNullOrEmpty(quickFilter);

            if (!hasFilters)
            {
                await SetEmptyViewBag(pageSize);
                return View(new List<AttendanceViewModel>());
            }

            var logs = await _queryService.GetFilteredLogs(
                searchString, fromDate, toDate, branchId, deviceId);

            var employeeIds = logs.Select(l => l.BiometricUserId).Distinct().ToList();
            var employees = await _queryService.GetEmployeesDictionary(employeeIds);

            var branchIds = logs.Select(l => l.BranchId).Distinct().ToList();
            var branches = await _queryService.GetBranchesDictionary(branchIds);

            var deviceIds = logs.Select(l => l.DeviceId).Distinct().ToList();
            var devices = await _queryService.GetDevicesDictionary(deviceIds);

            var viewModelList = await _calculationService.BuildAttendanceViewModels(
                logs, employees, branches, devices);

            viewModelList = ApplyAdditionalFilters(
                viewModelList, attendanceStatus, minWorkHours, maxWorkHours);

            var orderedList = viewModelList
                .OrderByDescending(x => x.Date)
                .ThenBy(x => x.BiometricUserId)
                .ToList();

            var pagedLogs = orderedList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            await SetViewBagData(
                viewModelList, pageNumber, pageSize, searchString, fromDate, toDate,
                branchId, deviceId, attendanceStatus, minWorkHours, maxWorkHours, quickFilter);

            return View(pagedLogs);
        }

        // ═══════════════════════════════════════════════════════
        // Helper Methods
        // ═══════════════════════════════════════════════════════

        private async Task SetEmptyViewBag(int pageSize)
        {
            ViewBag.TotalRecords = 0;
            ViewBag.TotalCheckIns = 0;
            ViewBag.TotalCheckOuts = 0;
            ViewBag.TotalFullAttendance = 0;
            ViewBag.TotalPartialAttendance = 0;
            ViewBag.AverageWorkHours = 0;
            ViewBag.CurrentPage = 1;
            ViewBag.TotalPages = 0;
            ViewBag.PageSize = pageSize;
            ViewBag.Branches = await _lookupService.GetActiveBranchesAsync();
            ViewBag.Devices = await _lookupService.GetActiveDevicesAsync();
        }

        private async Task SetViewBagData(
            List<AttendanceViewModel> fullList,
            int pageNumber,
            int pageSize,
            string? searchString,
            DateTime? fromDate,
            DateTime? toDate,
            int? branchId,
            int? deviceId,
            string? attendanceStatus,
            int? minWorkHours,
            int? maxWorkHours,
            string? quickFilter)
        {
            ViewBag.TotalRecords = fullList.Count;
            ViewBag.TotalCheckIns = fullList.Count(v => v.CheckInTime.HasValue);
            ViewBag.TotalCheckOuts = fullList.Count(v => v.CheckOutTime.HasValue);
            ViewBag.TotalFullAttendance = fullList.Count(v => v.Status == "حضور كامل");
            ViewBag.TotalPartialAttendance = fullList.Count(v => v.Status == "دخول فقط");

            var workHoursList = fullList.Where(v => v.WorkingHours > 0).ToList();
            ViewBag.AverageWorkHours = workHoursList.Any()
                ? workHoursList.Average(v => v.WorkingHours)
                : 0;

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(fullList.Count / (double)pageSize);
            ViewBag.PageSize = pageSize;

            ViewBag.Branches = await _lookupService.GetActiveBranchesAsync();
            ViewBag.Devices = await _lookupService.GetActiveDevicesAsync();

            ViewBag.SearchString = searchString;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.BranchId = branchId;
            ViewBag.DeviceId = deviceId;
            ViewBag.AttendanceStatus = attendanceStatus;
            ViewBag.MinWorkHours = minWorkHours;
            ViewBag.MaxWorkHours = maxWorkHours;
            ViewBag.QuickFilter = quickFilter;
        }

        /// <summary>
        /// فلتر سريع حسب الفترة (اليوم، الأسبوع، الشهر، إلخ)
        /// </summary>
        private void ApplyQuickFilter(string? quickFilter, ref DateTime? fromDate, ref DateTime? toDate)
        {
            if (string.IsNullOrEmpty(quickFilter))
                return;

            var today = DateTime.Today;

            switch (quickFilter.ToLower())
            {
                case "today":
                    fromDate = today;
                    toDate = today;
                    break;

                case "yesterday":
                    fromDate = today.AddDays(-1);
                    toDate = today.AddDays(-1);
                    break;

                case "thisweek":
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                    fromDate = startOfWeek;
                    toDate = today;
                    break;

                case "lastweek":
                    var lastWeekStart = today.AddDays(-(int)today.DayOfWeek - 7);
                    var lastWeekEnd = lastWeekStart.AddDays(6);
                    fromDate = lastWeekStart;
                    toDate = lastWeekEnd;
                    break;

                case "thismonth":
                    fromDate = new DateTime(today.Year, today.Month, 1);
                    toDate = today;
                    break;

                case "lastmonth":
                    var lastMonth = today.AddMonths(-1);
                    fromDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    toDate = new DateTime(lastMonth.Year, lastMonth.Month,
                        DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;

                case "last7days":
                    fromDate = today.AddDays(-7);
                    toDate = today;
                    break;

                case "last30days":
                    fromDate = today.AddDays(-30);
                    toDate = today;
                    break;
            }
        }

        /// <summary>
        /// فلاتر إضافية على ViewModel
        /// </summary>
        private List<AttendanceViewModel> ApplyAdditionalFilters(
            List<AttendanceViewModel> list,
            string? attendanceStatus,
            int? minWorkHours,
            int? maxWorkHours)
        {
            // فلتر حسب حالة الحضور
            if (!string.IsNullOrEmpty(attendanceStatus))
            {
                list = list.Where(v => v.Status == attendanceStatus).ToList();
            }

            // فلتر حسب ساعات العمل
            if (minWorkHours.HasValue)
            {
                list = list.Where(v => v.WorkingHours >= minWorkHours.Value).ToList();
            }

            if (maxWorkHours.HasValue)
            {
                list = list.Where(v => v.WorkingHours <= maxWorkHours.Value).ToList();
            }

            return list;
        }
    }
}
