using Microsoft.Extensions.Caching.Memory;
using ZKAttendanceWeb.Data.Repositories;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.WorkShifts
{
    public class WorkShiftService : IWorkShiftService
    {
        private readonly WorkShiftRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WorkShiftService> _logger;

        public WorkShiftService(
            WorkShiftRepository repository,
            IMemoryCache cache,
            ILogger<WorkShiftService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<WorkShift>> GetAllShiftsAsync()
        {
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب قائمة الشفتات");
                throw;
            }
        }

        public async Task<WorkShift?> GetShiftByIdAsync(int shiftId)
        {
            try
            {
                return await _repository.GetByIdAsync(shiftId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الشفت {ShiftId}", shiftId);
                throw;
            }
        }

        public async Task<WorkShift> CreateShiftAsync(WorkShift shift)
        {
            try
            {
                // التحقق من عدم تكرار الاسم
                if (await _repository.NameExistsAsync(shift.ShiftName))
                {
                    throw new InvalidOperationException(
                        $"اسم الشفت '{shift.ShiftName}' موجود مسبقاً");
                }

                // التحقق من صحة الأوقات
                ValidateShiftTimes(shift);

                shift.IsActive = true;
                shift.CreatedDate = DateTime.Now;

                var result = await _repository.AddAsync(shift);

                ClearShiftCache();
                _logger.LogInformation("تم إنشاء شفت جديد: {ShiftName}", shift.ShiftName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء شفت جديد: {ShiftName}", shift.ShiftName);
                throw;
            }
        }

        public async Task<WorkShift> UpdateShiftAsync(WorkShift shift)
        {
            try
            {
                if (!await _repository.ExistsAsync(shift.ShiftId))
                {
                    throw new InvalidOperationException("الشفت غير موجود");
                }

                // التحقق من عدم تكرار الاسم
                if (await _repository.NameExistsAsync(shift.ShiftName, shift.ShiftId))
                {
                    throw new InvalidOperationException(
                        $"اسم الشفت '{shift.ShiftName}' موجود مسبقاً");
                }

                // التحقق من صحة الأوقات
                ValidateShiftTimes(shift);

                shift.ModifiedDate = DateTime.Now;
                var result = await _repository.UpdateAsync(shift);

                ClearShiftCache();
                _logger.LogInformation("تم تحديث الشفت: {ShiftName}", shift.ShiftName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث الشفت {ShiftId}", shift.ShiftId);
                throw;
            }
        }

        public async Task DeleteShiftAsync(int shiftId)
        {
            try
            {
                // التحقق من عدم وجود موظفين مرتبطين بالشفت
                var employeeCount = await _repository.GetEmployeeCountAsync(shiftId);
                if (employeeCount > 0)
                {
                    throw new InvalidOperationException(
                        $"لا يمكن حذف الشفت لأنه مرتبط بـ {employeeCount} موظف");
                }

                await _repository.SoftDeleteAsync(shiftId);

                ClearShiftCache();
                _logger.LogInformation("تم حذف الشفت {ShiftId}", shiftId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الشفت {ShiftId}", shiftId);
                throw;
            }
        }

        public async Task<bool> CanDeleteShiftAsync(int shiftId)
        {
            try
            {
                var employeeCount = await _repository.GetEmployeeCountAsync(shiftId);
                return employeeCount == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من إمكانية حذف الشفت {ShiftId}", shiftId);
                throw;
            }
        }

        public async Task<List<WorkShift>> GetActiveShiftsForTimeAsync(TimeSpan currentTime)
        {
            try
            {
                return await _repository.GetActiveShiftsForTimeAsync(currentTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الشفتات النشطة للوقت {Time}", currentTime);
                throw;
            }
        }

        private void ValidateShiftTimes(WorkShift shift)
        {
            // التحقق من أن CheckInWindowStart قبل StartTime
            if (shift.CheckInWindowStart.HasValue && shift.CheckInWindowStart.Value > shift.StartTime)
            {
                throw new InvalidOperationException(
                    "بداية نافذة الدخول يجب أن تكون قبل وقت بداية الشفت");
            }

            // التحقق من أن CheckOutWindowEnd بعد EndTime
            if (shift.CheckOutWindowEnd.HasValue && shift.CheckOutWindowEnd.Value < shift.EndTime)
            {
                throw new InvalidOperationException(
                    "نهاية نافذة الخروج يجب أن تكون بعد وقت نهاية الشفت");
            }

            // التحقق من صحة Break Time
            if (shift.BreakMinutes < 0)
            {
                throw new InvalidOperationException("وقت الاستراحة لا يمكن أن يكون سالباً");
            }

            // التحقق من صحة Late/Early Minutes
            if (shift.LateMinutes < 0 || shift.EarlyMinutes < 0)
            {
                throw new InvalidOperationException("دقائق التأخير والخروج المبكر لا يمكن أن تكون سالبة");
            }
        }

        private void ClearShiftCache()
        {
            _cache.Remove("active_shifts");
            _cache.Remove("shifts");
        }
    }
}
