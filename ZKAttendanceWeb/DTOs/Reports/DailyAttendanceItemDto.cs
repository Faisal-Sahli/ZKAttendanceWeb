namespace ZKAttendanceWeb.DTOs.Reports
{

    /// <summary>
    /// DTO لسجل حضور موظف واحد في يوم محدد
    /// </summary>
    public class DailyAttendanceItemDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string BiometricUserId { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime? FirstCheckIn { get; set; }
        public DateTime? LastCheckOut { get; set; }
        public TimeSpan? TotalWorkHours { get; set; }
        public string Status { get; set; } = "غائب";

        /// <summary>
        /// ساعات العمل بصيغة قابلة للعرض (مثال: 8:30)
        /// </summary>
        public string TotalWorkHoursFormatted => TotalWorkHours.HasValue
            ? $"{(int)TotalWorkHours.Value.TotalHours}:{TotalWorkHours.Value.Minutes:D2}"
            : "--";
    }
}
