namespace ZKAttendanceWeb.DTOs.Reports
{
    /// <summary>
    /// تقرير الحضور اليومي الكامل (للاستخدام في ReportService)
    /// </summary>
    public class DailyAttendanceReportDto
    {
        /// <summary>
        /// تاريخ التقرير
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// قائمة سجلات الحضور (الحاضرين والغائبين)
        /// </summary>
        public List<DailyAttendanceItemDto> Items { get; set; } = new();

        /// <summary>
        /// إجمالي عدد الموظفين
        /// </summary>
        public int TotalEmployees { get; set; }

        /// <summary>
        /// عدد الحاضرين
        /// </summary>
        public int PresentCount { get; set; }

        /// <summary>
        /// عدد الغائبين
        /// </summary>
        public int AbsentCount { get; set; }

        /// <summary>
        /// نسبة الحضور
        /// </summary>
        public double AttendanceRate => TotalEmployees > 0
            ? Math.Round((double)PresentCount / TotalEmployees * 100, 2)
            : 0;
    }
}
