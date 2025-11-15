using ZKAttendanceWeb.DTOs.Reports;

namespace ZKAttendanceWeb.Services.Report
{
    public interface IReportService
    {
        Task<DailyAttendanceReportDto> GetDailyAttendanceReportAsync(DateTime date, int? branchId = null, string? department = null);

        // تم تعديل توقيع الدالة ليشمل الفلاتر المناسبة حسب الأجهزة والموظفين بدل string department
        Task<DailyAttendanceReportDto> GetDailyAttendanceRangeReportAsync(
            DateTime fromDate,
            DateTime toDate,
            int? branchId = null,
            int? deviceId = null,
            int? employeeId = null);
    }
}
