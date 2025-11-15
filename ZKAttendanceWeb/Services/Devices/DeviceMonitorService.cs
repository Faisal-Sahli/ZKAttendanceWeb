using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using ZKAttendanceWeb.Data;

namespace ZKAttendanceWeb.Services.Devices
{
    public class DeviceMonitorService : IDeviceMonitorService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DeviceMonitorService> _logger;

        public DeviceMonitorService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DeviceMonitorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task CheckDevicesStatusAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ZKAttendanceWebDbContext>();

                var devices = await context.Devices.ToListAsync();

                _logger.LogInformation("🔍 بدء فحص {Count} جهاز...", devices.Count);

                foreach (var device in devices)
                {
                    try
                    {
                        // فحص الاتصال بالجهاز
                        var isOnline = await CheckDeviceConnectionAsync(device.DeviceIP, device.DevicePort);

                        // تحديث الحالة
                        device.IsOnline = isOnline;
                        device.LastCheckTime = DateTime.Now;
                        device.ConnectionStatus = isOnline ? "Connected" : "Disconnected";

                        if (isOnline)
                        {
                            device.LastConnectionTime = DateTime.Now;
                        }

                        _logger.LogInformation(
                            "{Status} الجهاز: {DeviceName} ({IP}:{Port})",
                            isOnline ? "✅ متصل" : "❌ غير متصل",
                            device.DeviceName,
                            device.DeviceIP,
                            device.DevicePort
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ خطأ في فحص الجهاز {DeviceName}", device.DeviceName);
                        device.IsOnline = false;
                        device.LastCheckTime = DateTime.Now;
                        device.ConnectionStatus = "Error";
                    }
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("✅ تم تحديث حالة جميع الأجهزة بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في خدمة مراقبة الأجهزة");
            }
        }

        /// <summary>
        /// فحص الاتصال بالجهاز عن طريق Ping و TCP
        /// </summary>
        private async Task<bool> CheckDeviceConnectionAsync(string ipAddress, int port)
        {
            // الطريقة 1: Ping
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 2000); // timeout 2 ثانية

                if (reply.Status == IPStatus.Success)
                {
                    // الطريقة 2: فحص Port
                    return await CheckTcpPortAsync(ipAddress, port);
                }
            }
            catch
            {
                // إذا Ping فشل، نجرب TCP مباشرة
            }

            // إذا Ping فشل، نجرب TCP فقط
            return await CheckTcpPortAsync(ipAddress, port);
        }

        /// <summary>
        /// فحص إذا كان Port مفتوح على الجهاز
        /// </summary>
        private async Task<bool> CheckTcpPortAsync(string ipAddress, int port)
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(3000); // timeout 3 ثواني

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && client.Connected)
                {
                    return true;
                }
            }
            catch
            {
                // Connection failed
            }

            return false;
        }
    }
}
