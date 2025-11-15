namespace ZKAttendanceWeb.DTOs.Configuration
{
    /// <summary>
    /// DTO لمعلومات الفرع الأساسية
    /// </summary>
    public class BranchDto
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
