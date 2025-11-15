namespace ZKAttendanceWeb.Models
{
    public class AttendanceViewModel
    {
        public string BiometricUserId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public double WorkingHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ShiftName { get; set; }

    }
}
