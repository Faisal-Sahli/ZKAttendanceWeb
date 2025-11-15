using ZKAttendanceWeb.DTOs.Reports;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Attendances
{
    public interface IAttendanceService
    {
        Task<List<AttendanceLog>> GetAllAttendanceLogsAsync();
        Task<List<AttendanceLog>> GetAttendanceLogsByDateRangeAsync(DateTime? startDate, DateTime? endDate);
        Task<List<DailyAttendanceItemDto>> GetDailyAttendanceReportAsync(
              DateTime date,
              int? branchId = null,
              int? departmentId = null);

        Task<DailyAttendanceReportSummaryDto> GetDailyAttendanceReportSummaryAsync(
            DateTime date,
            int? branchId = null,
            int? departmentId = null);
    }
}
