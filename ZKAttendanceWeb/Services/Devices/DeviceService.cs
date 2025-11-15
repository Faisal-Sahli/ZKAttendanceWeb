using Microsoft.Extensions.Caching.Memory;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Data.Repositories;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Devices
{
    public class DeviceService : IDeviceService
    {
        private readonly DeviceRepository _repository;
        private readonly ZKAttendanceWebDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(
            DeviceRepository repository,
            ZKAttendanceWebDbContext context,
            IMemoryCache cache,
            ILogger<DeviceService> logger)
        {
            _repository = repository;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Models.Device>> GetAllDevicesAsync()
        {
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب قائمة الأجهزة");
                throw;
            }
        }

        public async Task<Models.Device?> GetDeviceByIdAsync(int deviceId)
        {
            try
            {
                return await _repository.GetByIdAsync(deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الجهاز {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<List<Models.Device>> GetDevicesByBranchIdAsync(int branchId)
        {
            try
            {
                return await _repository.GetByBranchIdAsync(branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب أجهزة الفرع {BranchId}", branchId);
                throw;
            }
        }

        public async Task<Models.Device> CreateDeviceAsync(Models.Device device)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // التحقق من عدم تكرار IP + Port
                if (await _repository.IpPortExistsAsync(device.DeviceIP, device.DevicePort))
                {
                    throw new InvalidOperationException(
                        $"يوجد جهاز آخر بنفس عنوان IP ({device.DeviceIP}:{device.DevicePort})");
                }

                device.IsActive = true;
                device.CreatedDate = DateTime.Now;

                var createdDevice = await _repository.AddAsync(device);

                // ✅ إنشاء DeviceStatus أولي
                var initialStatus = new DeviceStatus
                {
                    DeviceId = createdDevice.DeviceId,
                    BranchId = createdDevice.BranchId,
                    IsOnline = false,
                    StatusTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    StatusMessage = "تم إنشاء الجهاز"
                };

                _context.DeviceStatuses.Add(initialStatus);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                ClearDeviceCache();
                _logger.LogInformation("تم إنشاء جهاز جديد: {DeviceName}", device.DeviceName);

                return createdDevice;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "خطأ في إنشاء جهاز جديد: {DeviceName}", device.DeviceName);
                throw;
            }
        }

        public async Task<Models.Device> UpdateDeviceAsync(Models.Device device)
        {
            try
            {
                if (!await _repository.ExistsAsync(device.DeviceId))
                {
                    throw new InvalidOperationException("الجهاز غير موجود");
                }

                // التحقق من عدم تكرار IP + Port
                if (await _repository.IpPortExistsAsync(device.DeviceIP, device.DevicePort, device.DeviceId))
                {
                    throw new InvalidOperationException(
                        $"يوجد جهاز آخر بنفس عنوان IP ({device.DeviceIP}:{device.DevicePort})");
                }

                device.ModifiedDate = DateTime.Now;
                var result = await _repository.UpdateAsync(device);

                ClearDeviceCache();
                _logger.LogInformation("تم تحديث الجهاز: {DeviceName}", device.DeviceName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث الجهاز {DeviceId}", device.DeviceId);
                throw;
            }
        }

        public async Task DeleteDeviceAsync(int deviceId)
        {
            try
            {
                await _repository.SoftDeleteAsync(deviceId);
                ClearDeviceCache();
                _logger.LogInformation("تم حذف الجهاز {DeviceId}", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الجهاز {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<(bool Success, string Message)> TestDeviceConnectionAsync(int deviceId)
        {
            try
            {
                var device = await _repository.GetByIdAsync(deviceId);
                if (device == null)
                {
                    return (false, "الجهاز غير موجود");
                }

                // هنا يمكنك إضافة منطق الاتصال الفعلي بالجهاز
                // مثلاً استخدام zkemkeeper.dll
                // var zkDevice = new zkemkeeper.CZKEMClass();
                // bool connected = zkDevice.Connect_Net(device.DeviceIP, device.DevicePort);

                // محاكاة للتوضيح
                await _repository.UpdateConnectionStatusAsync(
                    deviceId,
                    true,
                    "تم الاتصال بنجاح (اختبار)"
                );

                return (true, "تم الاتصال بالجهاز بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في اختبار الاتصال بالجهاز {DeviceId}", deviceId);
                return (false, $"فشل الاتصال: {ex.Message}");
            }
        }

        public async Task<List<Models.Device>> GetOnlineDevicesAsync()
        {
            try
            {
                return await _repository.GetOnlineDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الأجهزة المتصلة");
                throw;
            }
        }

        public async Task<List<Models.Device>> GetOfflineDevicesAsync()
        {
            try
            {
                return await _repository.GetOfflineDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الأجهزة غير المتصلة");
                throw;
            }
        }

        private void ClearDeviceCache()
        {
            _cache.Remove("active_devices");
            _cache.Remove("devices");
        }
    }
}
