using ZKAttendanceWeb.Data;
using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Shifts
{
    public class ShiftAssignmentService
    {
        private readonly ZKAttendanceWebDbContext _context;

        public ShiftAssignmentService(ZKAttendanceWebDbContext context)
        {
            _context = context;
        }

        // جلب الشفت للموظف ليوم محدد
        public async Task<WorkShift?> GetEmployeeShiftForDate(int employeeId, DateTime date)
        {
            var assignment = await _context.EmployeeShiftAssignments
                .Include(e => e.Shift)
                .Where(e => e.EmployeeId == employeeId
                            && e.IsActive
                            && e.EffectiveFrom <= date
                            && (e.EffectiveTo == null || e.EffectiveTo >= date))
                .OrderByDescending(e => e.EffectiveFrom)
                .FirstOrDefaultAsync();

            return assignment?.Shift;
        }

    }
}
