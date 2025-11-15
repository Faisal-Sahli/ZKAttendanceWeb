using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.ViewModels
{
    public class EmployeeDetailsViewModel
    {
        public Employee Employee { get; set; } = new();

        // الفروع والأجهزة المرتبطة فقط
        public List<BranchWithDevicesViewModel> AssignedBranches { get; set; } = new();

        // ✅ Nested Classes داخل الـ main class
        public class BranchWithDevicesViewModel
        {
            public int BranchId { get; set; }
            public string BranchCode { get; set; } = string.Empty;
            public string BranchName { get; set; } = string.Empty;
            public string? City { get; set; }

            public List<DeviceInfoViewModel> Devices { get; set; } = new();
        }

        public class DeviceInfoViewModel
        {
            public int DeviceId { get; set; }
            public string DeviceName { get; set; } = string.Empty;
            public string DeviceIP { get; set; } = string.Empty;
            public string? SerialNumber { get; set; }  // ✅ إضافة SerialNumber
        }
    }
}
