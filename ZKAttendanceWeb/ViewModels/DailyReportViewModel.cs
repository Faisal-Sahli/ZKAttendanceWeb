using ZKAttendanceWeb.DTOs.Reports;

namespace ZKAttendanceWeb.ViewModels
{
    public class DailyReportViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public string? Department { get; set; }
        public DailyAttendanceReportDto? Report { get; set; }
        public List<string> AvailableDepartments { get; set; } = new List<string>();
    }
}
