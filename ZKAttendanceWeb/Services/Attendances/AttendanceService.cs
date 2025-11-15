using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data.Repositories;
using ZKAttendanceWeb.DTOs.Reports;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Services.Attendances;

namespace ZKAttendanceWeb.Services.Attendance
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AttendanceLogRepository _repository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(
            AttendanceLogRepository repository,
            EmployeeRepository employeeRepository,
            ILogger<AttendanceService> logger)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<List<AttendanceLog>> GetAllAttendanceLogsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var lastMonth = today.AddMonths(-1);
                return await _repository.GetByDateRangeAsync(lastMonth, today.AddDays(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب سجلات الحضور");
                throw;
            }
        }

        public async Task<List<AttendanceLog>> GetAttendanceLogsByDateRangeAsync(
            DateTime? startDate,
            DateTime? endDate)
        {
            try
            {
                var from = startDate ?? DateTime.Today.AddMonths(-1);
                var to = (endDate ?? DateTime.Today).AddDays(1);

                return await _repository.GetByDateRangeAsync(from, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب سجلات الحضور من {StartDate} إلى {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<List<DailyAttendanceItemDto>> GetDailyAttendanceReportAsync(
    DateTime date,
    int? branchId = null,
    int? departmentId = null)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);

                var logs = await _repository.GetByDateRangeAsync(startDate, endDate, null, branchId);

                var grouped = logs
                    .GroupBy(l => l.BiometricUserId)
                    .Select(g => new DailyAttendanceItemDto  // ✅ غيّر هنا
                    {
                        EmployeeId = g.First().Employee?.EmployeeId ?? 0,  // ✅ أضف
                        BiometricUserId = g.Key,
                        EmployeeName = g.First().Employee?.EmployeeName ?? "غير معروف",
                        Department = g.First().Employee?.Department?.DepartmentName ?? "-",  // ✅ غيّر الاسم
                        FirstCheckIn = g.OrderBy(l => l.AttendanceTime).FirstOrDefault()?.AttendanceTime,  // ✅ غيّر
                        LastCheckOut = g.OrderByDescending(l => l.AttendanceTime).FirstOrDefault()?.AttendanceTime,  // ✅ غيّر
                        Status = "حاضر"  // ✅ أضف (يمكن تحديده بناءً على المنطق)
                    })
                    .OrderBy(r => r.EmployeeName)
                    .ToList();

                // حساب TotalWorkHours
                foreach (var item in grouped)
                {
                    if (item.FirstCheckIn.HasValue && item.LastCheckOut.HasValue)
                    {
                        item.TotalWorkHours = item.LastCheckOut.Value - item.FirstCheckIn.Value;
                    }
                }

                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء تقرير الحضور اليومي للتاريخ {Date}", date);
                throw;
            }
        }


        public async Task<DailyAttendanceReportSummaryDto> GetDailyAttendanceReportSummaryAsync(
    DateTime date,
    int? branchId = null,        // نستقبله لكن ما نستخدمه
    int? departmentId = null)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);

                // 1. جلب جميع الموظفين النشطين (حسب DepartmentId فقط)
                var allEmployees = await _employeeRepository.GetFilteredEmployeesAsync(departmentId);

                // 2. جلب سجلات الحضور لهذا اليوم
                var attendanceLogs = await _repository.GetByDateRangeAsync(startDate, endDate, null, branchId);

                // 3. تحديد الموظفين الحاضرين
                var presentEmployeeIds = attendanceLogs
                    .Select(l => l.BiometricUserId)
                    .Distinct()
                    .ToList();

                // 4. بناء قائمة الحاضرين
                var presentEmployees = new List<PresentEmployeeDto>();
                foreach (var biometricId in presentEmployeeIds)
                {
                    var employee = allEmployees.FirstOrDefault(e => e.BiometricUserId == biometricId);
                    if (employee == null) continue;

                    var employeeLogs = attendanceLogs
                        .Where(l => l.BiometricUserId == biometricId)
                        .OrderBy(l => l.AttendanceTime)
                        .ToList();

                    var checkIn = employeeLogs.FirstOrDefault()?.AttendanceTime;
                    var checkOut = employeeLogs.LastOrDefault()?.AttendanceTime;

                    TimeSpan? workDuration = null;
                    if (checkIn.HasValue && checkOut.HasValue && checkOut > checkIn)
                    {
                        workDuration = checkOut.Value - checkIn.Value;
                    }

                    // تحديد الحالة
                    bool isLate = false;
                    bool isEarlyLeave = false;
                    string status = "في الوقت";

                    if (checkIn.HasValue && checkIn.Value.TimeOfDay > new TimeSpan(8, 0, 0))
                    {
                        isLate = true;
                        status = "متأخر";
                    }

                    presentEmployees.Add(new PresentEmployeeDto
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeNumber = employee.SSN ?? "-",  // ✅ استخدم SSN بدل EmployeeNumber
                        EmployeeName = employee.EmployeeName,
                        BiometricUserId = employee.BiometricUserId,
                        DepartmentName = employee.Department?.DepartmentName ?? "-",
                        CheckInTime = checkIn,
                        CheckOutTime = checkOut,
                        WorkDuration = workDuration,
                        Status = status,
                        IsLate = isLate,
                        IsEarlyLeave = isEarlyLeave
                    });
                }

                // 5. بناء قائمة الغائبين
                var absentEmployees = allEmployees
                    .Where(e => !presentEmployeeIds.Contains(e.BiometricUserId))
                    .Select(e => new AbsentEmployeeDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeNumber = e.SSN ?? "-",  // ✅ استخدم SSN
                        EmployeeName = e.EmployeeName,
                        BiometricUserId = e.BiometricUserId,
                        DepartmentName = e.Department?.DepartmentName ?? "-",
                        PhoneNumber = e.PhoneNumber,
                        AbsentReason = "غياب بدون سبب"
                    })
                    .ToList();

                // 6. بناء الملخص النهائي
                var summary = new DailyAttendanceReportSummaryDto
                {
                    ReportDate = date,
                    TotalEmployees = allEmployees.Count,
                    PresentCount = presentEmployees.Count,
                    AbsentCount = absentEmployees.Count,
                    LateCount = presentEmployees.Count(p => p.IsLate),
                    EarlyLeaveCount = presentEmployees.Count(p => p.IsEarlyLeave),
                    PresentEmployees = presentEmployees.OrderBy(p => p.EmployeeName).ToList(),
                    AbsentEmployees = absentEmployees.OrderBy(a => a.EmployeeName).ToList()
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء ملخص تقرير الحضور للتاريخ {Date}", date);
                throw;
            }
        }

    }
}
