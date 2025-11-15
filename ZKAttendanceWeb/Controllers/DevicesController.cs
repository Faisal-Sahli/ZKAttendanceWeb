using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Services.Devices;
using ZKAttendanceWeb.Services.Common;

namespace ZKAttendanceWeb.Controllers
{
    public class DevicesController : Controller
    {
        private readonly IDeviceService _deviceService;
        private readonly LookupService _lookupService;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(
            IDeviceService deviceService,
            LookupService lookupService,
            ILogger<DevicesController> logger)
        {
            _deviceService = deviceService;
            _lookupService = lookupService;
            _logger = logger;
        }

        // GET: Devices
        public async Task<IActionResult> Index()
        {
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync();
                return View(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض قائمة الأجهزة");
                TempData["Error"] = "حدث خطأ أثناء تحميل قائمة الأجهزة";
                return View(new List<Device>());
            }
        }

        // GET: Devices/Create
        public async Task<IActionResult> Create()
        {
            await PopulateBranches();
            return View();
        }

        // POST: Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Device device)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _deviceService.CreateDeviceAsync(device);
                    TempData["Success"] = "تم إنشاء الجهاز بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء جهاز");
                    ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الجهاز");
                }
            }

            await PopulateBranches(device.BranchId);
            return View(device);
        }

        // GET: Devices/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceByIdAsync(id);
                if (device == null)
                {
                    TempData["Error"] = "الجهاز غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateBranches(device.BranchId);
                return View(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الجهاز {DeviceId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل بيانات الجهاز";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Devices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Device device)
        {
            if (id != device.DeviceId)
            {
                TempData["Error"] = "بيانات غير صحيحة";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _deviceService.UpdateDeviceAsync(device);
                    TempData["Success"] = "تم تحديث الجهاز بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحديث الجهاز {DeviceId}", id);
                    ModelState.AddModelError("", "حدث خطأ أثناء تحديث الجهاز");
                }
            }

            await PopulateBranches(device.BranchId);
            return View(device);
        }

        // GET: Devices/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceByIdAsync(id);
                if (device == null)
                {
                    TempData["Error"] = "الجهاز غير موجود";
                    return RedirectToAction(nameof(Index));
                }
                return View(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض تفاصيل الجهاز {DeviceId}", id);
                TempData["Error"] = "حدث خطأ أثناء عرض تفاصيل الجهاز";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Devices/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _deviceService.DeleteDeviceAsync(id);
                TempData["Success"] = "تم حذف الجهاز بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الجهاز {DeviceId}", id);
                TempData["Error"] = "حدث خطأ أثناء حذف الجهاز";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Devices/TestConnection/5
        [HttpPost]
        public async Task<IActionResult> TestConnection(int id)
        {
            try
            {
                var result = await _deviceService.TestDeviceConnectionAsync(id);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في اختبار الاتصال بالجهاز {DeviceId}", id);
                return Json(new { success = false, message = "حدث خطأ أثناء اختبار الاتصال" });
            }
        }

        // ✅ NEW: API endpoint for real-time status updates
        [HttpGet]
        public async Task<IActionResult> GetDevicesStatus()
        {
            try
            {
                var devices = await _deviceService.GetAllDevicesAsync();

                var statusList = devices.Select(d => new
                {
                    deviceId = d.DeviceId,
                    isOnline = d.IsOnline,
                    lastConnectionTime = d.LastConnectionTime?.ToString("yyyy/MM/dd hh:mm tt"),
                    connectionStatus = d.ConnectionStatus,
                    isActive = d.IsActive
                });

                var stats = new
                {
                    total = devices.Count,
                    online = devices.Count(d => d.IsOnline && d.IsActive),
                    offline = devices.Count(d => !d.IsOnline && d.IsActive),
                    inactive = devices.Count(d => !d.IsActive)
                };

                return Json(new { success = true, devices = statusList, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب حالة الأجهزة");
                return Json(new { success = false, message = "حدث خطأ" });
            }
        }

        // Helper method
        private async Task PopulateBranches(int? selectedBranchId = null)
        {
            var branches = await _lookupService.GetActiveBranchesAsync();
            ViewBag.Branches = new SelectList(branches, "BranchId", "BranchName", selectedBranchId);
        }
    }
}
