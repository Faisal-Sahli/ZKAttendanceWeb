using Microsoft.Extensions.Caching.Memory;
using ZKAttendanceWeb.Data.Repositories;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Departments
{
    public class DepartmentService : IDepartmentService
    {
        private readonly DepartmentRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            DepartmentRepository repository,
            IMemoryCache cache,
            ILogger<DepartmentService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Department>> GetAllDepartmentsAsync()
        {
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب قائمة الأقسام");
                throw;
            }
        }

        public async Task<Department?> GetDepartmentByIdAsync(int departmentId)
        {
            try
            {
                return await _repository.GetByIdAsync(departmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب القسم {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<List<Department>> GetTopLevelDepartmentsAsync()
        {
            try
            {
                return await _repository.GetTopLevelDepartmentsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الأقسام الرئيسية");
                throw;
            }
        }

        public async Task<List<Department>> GetSubDepartmentsAsync(int parentDepartmentId)
        {
            try
            {
                return await _repository.GetSubDepartmentsAsync(parentDepartmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في جلب الأقسام الفرعية للقسم {DepartmentId}", parentDepartmentId);
                throw;
            }
        }

        public async Task<Department> CreateDepartmentAsync(Department department)
        {
            try
            {
                // التحقق من عدم تكرار الكود
                if (!string.IsNullOrEmpty(department.DepartmentCode))
                {
                    if (await _repository.CodeExistsAsync(department.DepartmentCode))
                    {
                        throw new InvalidOperationException(
                            $"كود القسم '{department.DepartmentCode}' موجود مسبقاً");
                    }
                }

                department.IsActive = true;
                department.CreatedDate = DateTime.Now;

                var result = await _repository.AddAsync(department);

                ClearDepartmentCache();
                _logger.LogInformation("تم إنشاء قسم جديد: {DepartmentName}", department.DepartmentName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء قسم جديد: {DepartmentName}", department.DepartmentName);
                throw;
            }
        }

        public async Task<Department> UpdateDepartmentAsync(Department department)
        {
            try
            {
                if (!await _repository.ExistsAsync(department.DepartmentId))
                {
                    throw new InvalidOperationException("القسم غير موجود");
                }

                // التحقق من عدم تكرار الكود
                if (!string.IsNullOrEmpty(department.DepartmentCode))
                {
                    if (await _repository.CodeExistsAsync(department.DepartmentCode, department.DepartmentId))
                    {
                        throw new InvalidOperationException(
                            $"كود القسم '{department.DepartmentCode}' موجود مسبقاً");
                    }
                }

                // التحقق من عدم جعل القسم ابناً لنفسه
                if (department.ParentDepartmentId == department.DepartmentId)
                {
                    throw new InvalidOperationException("لا يمكن جعل القسم فرعاً لنفسه");
                }

                department.ModifiedDate = DateTime.Now;
                var result = await _repository.UpdateAsync(department);

                ClearDepartmentCache();
                _logger.LogInformation("تم تحديث القسم: {DepartmentName}", department.DepartmentName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث القسم {DepartmentId}", department.DepartmentId);
                throw;
            }
        }

        public async Task DeleteDepartmentAsync(int departmentId)
        {
            try
            {
                // التحقق من عدم وجود موظفين في القسم
                var employeeCount = await _repository.GetEmployeeCountAsync(departmentId);
                if (employeeCount > 0)
                {
                    throw new InvalidOperationException(
                        $"لا يمكن حذف القسم لأنه يحتوي على {employeeCount} موظف");
                }

                // التحقق من عدم وجود أقسام فرعية
                var subDepartments = await _repository.GetSubDepartmentsAsync(departmentId);
                if (subDepartments.Any())
                {
                    throw new InvalidOperationException(
                        $"لا يمكن حذف القسم لأنه يحتوي على {subDepartments.Count} قسم فرعي");
                }

                await _repository.SoftDeleteAsync(departmentId);

                ClearDepartmentCache();
                _logger.LogInformation("تم حذف القسم {DepartmentId}", departmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف القسم {DepartmentId}", departmentId);
                throw;
            }
        }

        public async Task<bool> CanDeleteDepartmentAsync(int departmentId)
        {
            try
            {
                var employeeCount = await _repository.GetEmployeeCountAsync(departmentId);
                var subDepartments = await _repository.GetSubDepartmentsAsync(departmentId);

                return employeeCount == 0 && !subDepartments.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من إمكانية حذف القسم {DepartmentId}", departmentId);
                throw;
            }
        }

        private void ClearDepartmentCache()
        {
            _cache.Remove("active_departments");
            _cache.Remove("departments");
        }
    }
}
