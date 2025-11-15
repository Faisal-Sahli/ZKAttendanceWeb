namespace ZKAttendanceWeb.Services.Devices
{
    public class DeviceMonitorBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeviceMonitorBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // كل دقيقة

        public DeviceMonitorBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DeviceMonitorBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ خدمة مراقبة الأجهزة بدأت - الفحص كل {Interval} دقيقة",
                _checkInterval.TotalMinutes);

            // انتظر 10 ثواني قبل البدء (لضمان تشغيل التطبيق بالكامل)
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            // استخدم PeriodicTimer (متوفر في .NET 6+)
            using var timer = new PeriodicTimer(_checkInterval);

            try
            {
                // تشغيل الفحص أول مرة فوراً
                await RunCheckAsync();

                // ثم كل دقيقة
                while (!stoppingToken.IsCancellationRequested
                       && await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunCheckAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("⏹️ خدمة مراقبة الأجهزة توقفت");
            }
        }

        private async Task RunCheckAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var monitorService = scope.ServiceProvider.GetRequiredService<IDeviceMonitorService>();

                await monitorService.CheckDevicesStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء تنفيذ فحص الأجهزة");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 إيقاف خدمة مراقبة الأجهزة...");
            return base.StopAsync(cancellationToken);
        }
    }
}
