using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.ViewModels;
using ZKAttendanceWeb.Services.Shifts;

namespace ZKAttendanceWeb.Services.Attendances
{

    //مسؤول عن: حساب الحضور والانصراف وساعات العمل وبناء AttendanceViewModel.


    public class AttendanceCalculationService
    {
        private readonly ShiftAssignmentService _shiftService;

        public AttendanceCalculationService(ShiftAssignmentService shiftService)
        {
            _shiftService = shiftService;
        }

        public async Task<List<AttendanceViewModel>> BuildAttendanceViewModels(
            List<AttendanceLog> logs,
            Dictionary<string, Employee> employees,
            Dictionary<int, string> branches,
            Dictionary<int, string> devices)
        {
            var groupedLogs = logs
                .GroupBy(l => new { l.BiometricUserId, Date = l.AttendanceTime.Date })
                .Select(g => new
                {
                    BiometricUserId = g.Key.BiometricUserId,
                    Date = g.Key.Date,
                    Logs = g.ToList()
                })
                .ToList();

            var viewModels = new List<AttendanceViewModel>();

            foreach (var group in groupedLogs)
            {
                if (!employees.TryGetValue(group.BiometricUserId, out var emp))
                    continue;

                var checkIn = group.Logs.OrderBy(x => x.AttendanceTime).FirstOrDefault();
                var checkOut = group.Logs.Count > 1
                    ? group.Logs.OrderByDescending(x => x.AttendanceTime).FirstOrDefault()
                    : null;

                var shift = await _shiftService.GetEmployeeShiftForDate(emp.EmployeeId, group.Date)
                    ?? emp.DefaultShift;

                double workingHours = CalculateWorkingHours(checkIn, checkOut);
                var status = GetAttendanceStatus(checkIn, checkOut, workingHours);

                viewModels.Add(new AttendanceViewModel
                {
                    BiometricUserId = group.BiometricUserId,
                    EmployeeName = emp.EmployeeName,
                    Date = group.Date,
                    CheckInTime = checkIn?.AttendanceTime,
                    CheckOutTime = checkOut?.AttendanceTime,
                    BranchName = checkIn != null && branches.ContainsKey(checkIn.BranchId)
                        ? branches[checkIn.BranchId] : "-",
                    DeviceName = checkIn != null && devices.ContainsKey(checkIn.DeviceId)
                        ? devices[checkIn.DeviceId] : "-",
                    WorkingHours = workingHours,
                    Status = status,
                    ShiftName = shift?.ShiftName ?? "-"
                });
            }

            return viewModels;
        }

        private double CalculateWorkingHours(AttendanceLog? checkIn, AttendanceLog? checkOut)
        {
            if (checkIn == null || checkOut == null)
                return 0;

            var timeDiff = (checkOut.AttendanceTime - checkIn.AttendanceTime).TotalMinutes;
            return timeDiff < 30 ? 0 : (checkOut.AttendanceTime - checkIn.AttendanceTime).TotalHours;
        }

        private string GetAttendanceStatus(AttendanceLog? checkIn, AttendanceLog? checkOut, double workingHours)
        {
            if (checkIn == null && checkOut != null)
                return "خروج فقط ⚠";

            if (checkIn == null)
                return "غياب";

            if (checkOut == null || workingHours == 0)
                return "دخول فقط";

            if (workingHours < 4)
                return "نصف يوم";

            return "حضور كامل";
        }
    }
}
