#nullable disable
using Microsoft.AspNetCore.Mvc;
using ZKAttendanceWeb.Services.Branches;
using ZKAttendanceWeb.Services.Devices;
using ZKAttendanceWeb.DTOs.Configuration;
using ZKAttendanceWeb.Models;                

namespace ZKAttendanceWeb.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly IBrancheService _branchService;
        private readonly IDeviceService _deviceService;
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(
            IBrancheService branchService,
            IDeviceService deviceService,
            ILogger<ConfigurationController> logger)
        {
            _branchService = branchService;
            _deviceService = deviceService;
            _logger = logger;
        }

        /// <summary>
        /// الحصول على معلومات فرع محدد مع أجهزته
        /// </summary>
        [HttpGet("Branch/{branchCode}")]
        [ProducesResponseType(typeof(BranchConfigurationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBranchConfiguration(string branchCode)
        {
            try
            {
                _logger.LogInformation("طلب معلومات الفرع: {BranchCode}", branchCode);

                // استخدام دالة مخصصة بدلاً من GetAll
                var branch = await _branchService.GetBranchByCodeAsync(branchCode);

                if (branch == null)
                {
                    _logger.LogWarning("الفرع غير موجود: {BranchCode}", branchCode);
                    return NotFound(new { message = $"الفرع {branchCode} غير موجود" });
                }

                var devices = await _deviceService.GetDevicesByBranchIdAsync(branch.BranchId);

                var response = new BranchConfigurationDto
                {
                    Branch = new BranchDto
                    {
                        BranchId = branch.BranchId,
                        BranchCode = branch.BranchCode,
                        BranchName = branch.BranchName,
                        City = branch.City
                    },
                    Devices = devices.Select(d => new DeviceDto
                    {
                        DeviceId = d.DeviceId,
                        DeviceName = d.DeviceName,
                        DeviceIP = d.DeviceIP,
                        DevicePort = d.DevicePort,
                        SerialNumber = d.SerialNumber,
                        DeviceModel = d.DeviceModel,
                        IsActive = d.IsActive
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب معلومات الفرع: {BranchCode}", branchCode);
                return StatusCode(500, new { message = "حدث خطأ في الخادم" });
            }
        }

        /// <summary>
        /// الحصول على جميع الفروع مع أجهزتها
        /// </summary>
        [HttpGet("AllBranches")]
        [ProducesResponseType(typeof(List<BranchConfigurationDto>), 200)]
        public async Task<IActionResult> GetAllBranchesConfiguration()
        {
            try
            {
                _logger.LogInformation("طلب جميع معلومات الفروع والأجهزة");

                // جلب جميع البيانات بـ query واحد (Include)
                var branchesWithDevices = await _branchService.GetAllBranchesWithDevicesAsync();

                var response = branchesWithDevices.Select(branch => new BranchConfigurationDto
                {
                    Branch = new BranchDto
                    {
                        BranchId = branch.BranchId,
                        BranchCode = branch.BranchCode,
                        BranchName = branch.BranchName,
                        City = branch.City
                    },
                    Devices = branch.Devices.Select(d => new DeviceDto
                    {
                        DeviceId = d.DeviceId,
                        DeviceName = d.DeviceName,
                        DeviceIP = d.DeviceIP,
                        DevicePort = d.DevicePort,
                        SerialNumber = d.SerialNumber,
                        DeviceModel = d.DeviceModel,
                        IsActive = d.IsActive
                    }).ToList()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب جميع معلومات الفروع");
                return StatusCode(500, new { message = "حدث خطأ في الخادم" });
            }
        }
    }
}
