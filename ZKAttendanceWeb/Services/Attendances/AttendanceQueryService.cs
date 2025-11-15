using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Attendances
{
    public class AttendanceQueryService
    {
        private readonly ZKAttendanceWebDbContext _context;

        public AttendanceQueryService(ZKAttendanceWebDbContext context)
        {
            _context = context;
        }

        public async Task<List<AttendanceLog>> GetFilteredLogs(
            string? searchString,
            DateTime? fromDate,
            DateTime? toDate,
            int? branchId,
            int? deviceId)
        {
            var query = _context.AttendanceLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(a => a.BiometricUserId.Contains(searchString));

            if (fromDate.HasValue)
                query = query.Where(a => a.AttendanceTime.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(a => a.AttendanceTime.Date <= toDate.Value.Date);

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(a => a.BranchId == branchId.Value);

            if (deviceId.HasValue && deviceId.Value > 0)
                query = query.Where(a => a.DeviceId == deviceId.Value);

            return await query.ToListAsync();
        }

        public async Task<Dictionary<string, Employee>> GetEmployeesDictionary(List<string> biometricIds)
        {
            return await _context.Employees
                .AsNoTracking()
                .Include(e => e.DefaultShift)
                .Where(e => biometricIds.Contains(e.BiometricUserId))
                .ToDictionaryAsync(e => e.BiometricUserId);
        }

        public async Task<Dictionary<int, string>> GetBranchesDictionary(List<int> branchIds)
        {
            return await _context.Branches
                .AsNoTracking()
                .Where(b => branchIds.Contains(b.BranchId))
                .ToDictionaryAsync(b => b.BranchId, b => b.BranchName);
        }

        public async Task<Dictionary<int, string>> GetDevicesDictionary(List<int> deviceIds)
        {
            return await _context.Devices
                .AsNoTracking()
                .Where(d => deviceIds.Contains(d.DeviceId))
                .ToDictionaryAsync(d => d.DeviceId, d => d.DeviceName);
        }
    }
}
