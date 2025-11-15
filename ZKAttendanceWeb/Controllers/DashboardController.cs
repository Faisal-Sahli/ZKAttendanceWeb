using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ZKAttendanceWebDbContext _context;

        public DashboardController(ZKAttendanceWebDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var today = DateTime.Today;

                // إحصائيات عامة
                ViewBag.TotalEmployees = await _context.Employees.CountAsync();
                ViewBag.TotalBranches = await _context.Branches.CountAsync();
                ViewBag.TotalDevices = await _context.Devices.CountAsync();
                ViewBag.ActiveDevices = await _context.Devices.CountAsync(d => d.IsActive);
                ViewBag.InactiveDevices = await _context.Devices.CountAsync(d => !d.IsActive);

                // إحصائيات الحضور اليوم
                var todayLogs = await _context.AttendanceLogs
                    .Where(a => a.AttendanceTime.Date == today)
                    .ToListAsync();

                var todayEmployeeIds = todayLogs
                    .Select(l => l.BiometricUserId)
                    .Distinct()
                    .Count();

                ViewBag.TodayAttendance = todayEmployeeIds;
                ViewBag.TodayAbsent = ViewBag.TotalEmployees - todayEmployeeIds;

                // حساب المتأخرين والمغادرة المبكرة (يمكن تحسينه حسب منطق الورديات)
                ViewBag.TodayLate = 0; // TODO: تطبيق منطق التأخير
                ViewBag.TodayEarlyLeave = 0; // TODO: تطبيق منطق المغادرة المبكرة

                // آخر وقت مزامنة
                ViewBag.LastSyncTime = await _context.AttendanceLogs
                    .Where(a => a.IsSynced)
                    .OrderByDescending(a => a.SyncedDate)
                    .Select(a => a.SyncedDate)
                    .FirstOrDefaultAsync();

                // آخر السجلات
                ViewBag.RecentLogs = await _context.AttendanceLogs
                    .Include(a => a.Branch)
                    .Include(a => a.Device)
                    .Where(a => a.AttendanceTime.Date == today)
                    .OrderByDescending(a => a.AttendanceTime)
                    .Take(10)
                    .ToListAsync();

                // أخطاء الأجهزة (✅ استخدام ErrorDateTime بدلاً من ErrorTime)
                ViewBag.DeviceErrors = await _context.DeviceErrors
                    .Include(e => e.Device)
                    .Where(e => !e.IsResolved)
                    .OrderByDescending(e => e.ErrorDateTime)
                    .Take(5)
                    .ToListAsync();

                return View();
            }
            catch (Exception ex)
            {
                // ✅ بدون استخدام _logger
                // يمكنك إضافة Logger لاحقاً إذا أردت
                Console.WriteLine($"خطأ في تحميل لوحة المعلومات: {ex.Message}");
                return View();
            }
        }
    }
}
