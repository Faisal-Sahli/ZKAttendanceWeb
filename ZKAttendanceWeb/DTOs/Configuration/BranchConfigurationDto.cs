namespace ZKAttendanceWeb.DTOs.Configuration
{
    /// <summary>
    /// DTO للإعدادات الكاملة لفرع مع أجهزته
    /// يستخدم في ConfigurationController
    /// </summary>
    public class BranchConfigurationDto
    {
        public BranchDto Branch { get; set; } = new();
        public List<DeviceDto> Devices { get; set; } = new();
    }

    

}
