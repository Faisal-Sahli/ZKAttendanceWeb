using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Branches
{
    public interface IBrancheService
    {
        Task<List<Branch>> GetAllBranchesAsync();
        Task<Branch?> GetBranchByIdAsync(int branchId);
        Task<Branch?> GetBranchByCodeAsync(string branchCode); // ✅ إضافة
        Task<List<Branch>> GetAllBranchesWithDevicesAsync();   // ✅ إضافة
        Task<Branch> CreateBranchAsync(Branch branch);
        Task<Branch> UpdateBranchAsync(Branch branch);
        Task DeleteBranchAsync(int branchId);
    }
}
