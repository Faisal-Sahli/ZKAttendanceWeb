using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Devices
{
    public interface IDeviceService
    {
        Task<List<Models.Device>> GetAllDevicesAsync();
        Task<Models.Device?> GetDeviceByIdAsync(int deviceId);
        Task<List<Models.Device>> GetDevicesByBranchIdAsync(int branchId);
        Task<Models.Device> CreateDeviceAsync(Models.Device device);
        Task<Models.Device> UpdateDeviceAsync(Models.Device device);
        Task DeleteDeviceAsync(int deviceId);
        Task<(bool Success, string Message)> TestDeviceConnectionAsync(int deviceId);
        Task<List<Models.Device>> GetOnlineDevicesAsync();
        Task<List<Models.Device>> GetOfflineDevicesAsync();
    }
}
