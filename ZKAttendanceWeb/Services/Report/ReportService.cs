using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.DTOs.Reports;

namespace ZKAttendanceWeb.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly ZKAttendanceWebDbContext _context;

        public ReportService(ZKAttendanceWebDbContext context)
        {
            _context = context;
        }

        public async Task<DailyAttendanceReportDto> GetDailyAttendanceReportAsync(DateTime date, int? branchId = null, string? department = null)
        {
            var startDate = date.Date;
            var endDate = date.Date.AddDays(1);

            var employeesQuery = _context.Employees
                .Include(e => e.Department)  // ✅ تحميل Department
                .Where(e => e.IsActive);

            // ✅ تصحيح مقارنة Department:
            if (!string.IsNullOrEmpty(department))
            {
                employeesQuery = employeesQuery.Where(e =>
                    e.Department != null && e.Department.DepartmentName == department);
            }

            var allEmployees = await employeesQuery.ToListAsync();

            var attendanceLogsQuery = _context.AttendanceLogs
                .Where(a => a.AttendanceTime >= startDate && a.AttendanceTime < endDate);

            if (branchId.HasValue)
            {
                attendanceLogsQuery = attendanceLogsQuery.Where(a => a.BranchId == branchId.Value);
            }

            var attendanceLogs = await attendanceLogsQuery.ToListAsync();

            // ✅ تصحيح الـ queries:
            var groupedAttendance = attendanceLogs
                .GroupBy(a => a.BiometricUserId)
                .Select(g => new
                {
                    BiometricUserId = g.Key,
                    FirstCheckIn = g.Where(x => x.AttendanceType == "CheckIn" || x.AttendanceType == null)
                                    .OrderBy(x => x.AttendanceTime)
                                    .FirstOrDefault()?.AttendanceTime,
                    LastCheckOut = g.Where(x => x.AttendanceType == "CheckOut")
                                    .OrderByDescending(x => x.AttendanceTime)
                                    .FirstOrDefault()?.AttendanceTime,
                    FirstRecord = g.OrderBy(x => x.AttendanceTime).FirstOrDefault()?.AttendanceTime,
                    LastRecord = g.OrderByDescending(x => x.AttendanceTime).FirstOrDefault()?.AttendanceTime
                })
                .ToDictionary(x => x.BiometricUserId);

            var items = new List<DailyAttendanceItemDto>();

            foreach (var employee in allEmployees)
            {
                var item = new DailyAttendanceItemDto
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.EmployeeName,
                    BiometricUserId = employee.BiometricUserId,
                    Department = employee.Department?.DepartmentName ?? "غير محدد"  // ✅ مُصحح
                };

                if (groupedAttendance.TryGetValue(employee.BiometricUserId, out var attendance))
                {
                    item.FirstCheckIn = attendance.FirstCheckIn ?? attendance.FirstRecord;
                    item.LastCheckOut = attendance.LastCheckOut ??
                                       (attendance.LastRecord != attendance.FirstRecord ? attendance.LastRecord : null);

                    if (item.FirstCheckIn.HasValue && item.LastCheckOut.HasValue)
                    {
                        item.TotalWorkHours = item.LastCheckOut.Value - item.FirstCheckIn.Value;
                        item.Status = "حاضر";
                    }
                    else if (item.FirstCheckIn.HasValue && !item.LastCheckOut.HasValue)
                    {
                        item.Status = "لم يسجل خروج";
                    }
                    else
                    {
                        item.Status = "غائب";
                    }
                }
                else
                {
                    item.Status = "غائب";
                }

                items.Add(item);
            }

            var report = new DailyAttendanceReportDto
            {
                Date = date.Date,
                Items = items.OrderBy(i => i.EmployeeName).ToList(),
                TotalEmployees = allEmployees.Count,
                PresentCount = items.Count(i => i.Status == "حاضر" || i.Status == "لم يسجل خروج"),
                AbsentCount = items.Count(i => i.Status == "غائب")
            };

            return report;
        }

        public async Task<DailyAttendanceReportDto> GetDailyAttendanceRangeReportAsync(DateTime fromDate, DateTime toDate, int? branchId = null, int? deviceId = null, int? employeeId = null)
        {
            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1);

            var employeesQuery = _context.Employees
                .Include(e => e.Department)  // ✅ تحميل Department
                .Where(e => e.IsActive);

            if (employeeId.HasValue)
                employeesQuery = employeesQuery.Where(e => e.EmployeeId == employeeId.Value);

            var allEmployees = await employeesQuery.ToListAsync();

            var attendanceLogsQuery = _context.AttendanceLogs
                .Where(a => a.AttendanceTime >= startDate && a.AttendanceTime < endDate);

            if (branchId.HasValue)
                attendanceLogsQuery = attendanceLogsQuery.Where(a => a.BranchId == branchId.Value);

            if (deviceId.HasValue)
                attendanceLogsQuery = attendanceLogsQuery.Where(a => a.DeviceId == deviceId.Value);

            var attendanceLogs = await attendanceLogsQuery.ToListAsync();

            // ✅ تصحيح:
            var groupedAttendance = attendanceLogs
                .GroupBy(a => a.BiometricUserId)
                .Select(g => new
                {
                    BiometricUserId = g.Key,
                    FirstCheckIn = g.Where(x => x.AttendanceType == "CheckIn" || x.AttendanceType == null)
                                    .OrderBy(x => x.AttendanceTime)
                                    .FirstOrDefault()?.AttendanceTime,
                    LastCheckOut = g.Where(x => x.AttendanceType == "CheckOut")
                                    .OrderByDescending(x => x.AttendanceTime)
                                    .FirstOrDefault()?.AttendanceTime,
                    FirstRecord = g.OrderBy(x => x.AttendanceTime).FirstOrDefault()?.AttendanceTime,
                    LastRecord = g.OrderByDescending(x => x.AttendanceTime).FirstOrDefault()?.AttendanceTime
                })
                .ToDictionary(x => x.BiometricUserId);

            var items = new List<DailyAttendanceItemDto>();

            foreach (var employee in allEmployees)
            {
                if (employeeId.HasValue && employee.EmployeeId != employeeId.Value)
                    continue;

                var item = new DailyAttendanceItemDto
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.EmployeeName,
                    BiometricUserId = employee.BiometricUserId,
                    Department = employee.Department?.DepartmentName ?? "غير محدد"  // ✅ مُصحح
                };

                if (groupedAttendance.TryGetValue(employee.BiometricUserId, out var attendance))
                {
                    item.FirstCheckIn = attendance.FirstCheckIn ?? attendance.FirstRecord;
                    item.LastCheckOut = attendance.LastCheckOut ??
                                       (attendance.LastRecord != attendance.FirstRecord ? attendance.LastRecord : null);

                    if (item.FirstCheckIn.HasValue && item.LastCheckOut.HasValue)
                    {
                        item.TotalWorkHours = item.LastCheckOut.Value - item.FirstCheckIn.Value;
                        item.Status = "حاضر";
                    }
                    else if (item.FirstCheckIn.HasValue && !item.LastCheckOut.HasValue)
                    {
                        item.Status = "لم يسجل خروج";
                    }
                    else
                    {
                        item.Status = "غائب";
                    }
                }
                else
                {
                    item.Status = "غائب";
                }

                items.Add(item);
            }

            var report = new DailyAttendanceReportDto
            {
                Date = fromDate.Date,
                Items = items.OrderBy(i => i.EmployeeName).ToList(),
                TotalEmployees = allEmployees.Count,
                PresentCount = items.Count(i => i.Status == "حاضر" || i.Status == "لم يسجل خروج"),
                AbsentCount = items.Count(i => i.Status == "غائب")
            };

            return report;
        }
    }
}
