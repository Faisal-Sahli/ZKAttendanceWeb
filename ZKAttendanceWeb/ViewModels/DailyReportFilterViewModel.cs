using System;
using System.Collections.Generic;
using ZKAttendanceWeb.DTOs.Reports;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.ViewModels
{
    public class DailyReportFilterViewModel
    {
        // خيارات الفلترة
        public int? SelectedBranchId { get; set; }
        public int? SelectedDeviceId { get; set; }
        public int? SelectedEmployeeId { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.Today.AddDays(-7);
        public DateTime DateTo { get; set; } = DateTime.Today;

        // قوائم الاختيار
        public List<Branch> Branches { get; set; } = new List<Branch>();
        public List<Device> Devices { get; set; } = new List<Device>();
        public List<Employee> Employees { get; set; } = new List<Employee>();

        // النتيجة المعروضة
        public DailyAttendanceReportDto? Report { get; set; }
    }
}
