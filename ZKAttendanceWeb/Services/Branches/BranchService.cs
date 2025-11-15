using Microsoft.Extensions.Caching.Memory;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.Repostries;

namespace ZKAttendanceWeb.Services.Branches
{
    public class BrancheService : IBrancheService
    {
        private readonly BranchRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BrancheService> _logger;

        public BrancheService(
            BranchRepository repository,
            IMemoryCache cache,
            ILogger<BrancheService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Branch>> GetAllBranchesAsync()
        {
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب قائمة الفروع");
                throw;
            }
        }

        public async Task<Branch?> GetBranchByIdAsync(int branchId)
        {
            try
            {
                return await _repository.GetByIdAsync(branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الفرع {BranchId}", branchId);
                throw;
            }
        }

        // ✅ إضافة: جلب فرع حسب الكود
        public async Task<Branch?> GetBranchByCodeAsync(string branchCode)
        {
            try
            {
                return await _repository.GetByCodeAsync(branchCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الفرع بالكود {BranchCode}", branchCode);
                throw;
            }
        }

        // ✅ إضافة: جلب جميع الفروع مع أجهزتها (بـ query واحد)
        public async Task<List<Branch>> GetAllBranchesWithDevicesAsync()
        {
            try
            {
                // استخدام Cache للاستعلامات الثقيلة
                return await _cache.GetOrCreateAsync("branches_with_devices", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    return await _repository.GetAllWithDevicesAsync();
                }) ?? new List<Branch>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الفروع مع الأجهزة");
                throw;
            }
        }

        public async Task<Branch> CreateBranchAsync(Branch branch)
        {
            try
            {
                if (await _repository.CodeExistsAsync(branch.BranchCode))
                {
                    throw new InvalidOperationException($"كود الفرع '{branch.BranchCode}' موجود مسبقاً");
                }

                branch.IsActive = true;
                branch.CreatedDate = DateTime.Now;

                var result = await _repository.AddAsync(branch);
                ClearBranchCache();

                _logger.LogInformation("تم إنشاء فرع جديد: {BranchName}", branch.BranchName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء فرع جديد: {BranchName}", branch.BranchName);
                throw;
            }
        }

        public async Task<Branch> UpdateBranchAsync(Branch branch)
        {
            try
            {
                if (!await _repository.ExistsAsync(branch.BranchId))
                {
                    throw new InvalidOperationException($"الفرع غير موجود");
                }

                if (await _repository.CodeExistsAsync(branch.BranchCode, branch.BranchId))
                {
                    throw new InvalidOperationException($"كود الفرع '{branch.BranchCode}' موجود مسبقاً");
                }

                branch.ModifiedDate = DateTime.Now;
                var result = await _repository.UpdateAsync(branch);
                ClearBranchCache();

                _logger.LogInformation("تم تحديث الفرع: {BranchName}", branch.BranchName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث الفرع {BranchId}", branch.BranchId);
                throw;
            }
        }

        public async Task DeleteBranchAsync(int branchId)
        {
            try
            {
                await _repository.SoftDeleteAsync(branchId);
                ClearBranchCache();
                _logger.LogInformation("تم حذف الفرع {BranchId}", branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الفرع {BranchId}", branchId);
                throw;
            }
        }

        private void ClearBranchCache()
        {
            _cache.Remove("active_branches");
            _cache.Remove("branches");
            _cache.Remove("branches_with_devices"); // ✅ إضافة
        }
    }
}
