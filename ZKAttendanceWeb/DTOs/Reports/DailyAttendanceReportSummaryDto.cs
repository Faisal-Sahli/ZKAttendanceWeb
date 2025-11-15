namespace ZKAttendanceWeb.DTOs.Reports
{
    /// <summary>
    /// ملخص شامل لتقرير الحضور اليومي
    /// </summary>
    public class DailyAttendanceReportSummaryDto
    {
        public DateTime ReportDate { get; set; }

        // الإحصائيات
        public int TotalEmployees { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int EarlyLeaveCount { get; set; }

        // قائمة الحاضرين
        public List<PresentEmployeeDto> PresentEmployees { get; set; } = new();

        // قائمة الغائبين
        public List<AbsentEmployeeDto> AbsentEmployees { get; set; } = new();
    }

    /// <summary>
    /// معلومات الموظف الحاضر
    /// </summary>
    public class PresentEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string BiometricUserId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? WorkDuration { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsLate { get; set; }
        public bool IsEarlyLeave { get; set; }

        /// <summary>
        /// ساعات العمل منسقة (مثال: 8:30)
        /// </summary>
        public string WorkDurationFormatted => WorkDuration.HasValue
            ? $"{(int)WorkDuration.Value.TotalHours}:{WorkDuration.Value.Minutes:D2}"
            : "--";
    }

    /// <summary>
    /// معلومات الموظف الغائب
    /// </summary>
    public class AbsentEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string BiometricUserId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string AbsentReason { get; set; } = "غياب بدون سبب";
    }
}
