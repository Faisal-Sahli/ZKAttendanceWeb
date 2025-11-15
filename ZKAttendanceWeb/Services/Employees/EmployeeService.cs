using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Data.Repositories;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Services.Employees
{
	public class EmployeeService : IEmployeeService
	{
		private readonly EmployeeRepository _repository;
		private readonly ILogger<EmployeeService> _logger;
		private readonly ZKAttendanceWebDbContext _context;

		public EmployeeService(
			EmployeeRepository repository,
			ILogger<EmployeeService> logger,
			ZKAttendanceWebDbContext context)
		{
			_repository = repository;
			_logger = logger;
			_context = context;
		}

		public async Task<List<Employee>> GetAllEmployeesAsync()
		{
			try
			{
				return await _repository.GetAllAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في جلب قائمة الموظفين");
				throw;
			}
		}

		public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
		{
			try
			{
				return await _repository.GetByIdAsync(employeeId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في جلب الموظف {EmployeeId}", employeeId);
				throw;
			}
		}

		public async Task<Employee?> GetEmployeeByBiometricIdAsync(string biometricUserId)
		{
			try
			{
				return await _repository.GetByBiometricIdAsync(biometricUserId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في جلب الموظف بـ BiometricId: {BiometricId}", biometricUserId);
				throw;
			}
		}

		public async Task<List<Employee>> GetEmployeesByDepartmentIdAsync(int departmentId)
		{
			try
			{
				return await _repository.GetByDepartmentIdAsync(departmentId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في جلب موظفي القسم {DepartmentId}", departmentId);
				throw;
			}
		}

		// ✅ الحصول على آخر رقم بصمة + 1 (من الموظفين + سجلات الحضور)
		public async Task<string> GetNextBiometricUserIdAsync()
		{
			try
			{
				// 1. أكبر رقم من جدول الموظفين
				var employees = await _repository.GetAllAsync();
				var maxEmployeeId = employees
					.Select(e => e.BiometricUserId)
					.Where(id => int.TryParse(id, out _))
					.Select(id => int.Parse(id))
					.DefaultIfEmpty(0)
					.Max();

				// 2. أكبر رقم من جدول سجلات الحضور
				var attendanceLogs = await _context.AttendanceLogs
					.Select(a => a.BiometricUserId)
					.Distinct()
					.ToListAsync(); // ✅ نجيب البيانات أولاً

				var maxAttendanceLogId = attendanceLogs
					.Where(id => int.TryParse(id, out _)) // ✅ الفلترة في Memory
					.Select(id => int.Parse(id))
					.DefaultIfEmpty(0)
					.Max();

				// 3. نختار الأكبر بينهم
				var maxId = Math.Max(maxEmployeeId, maxAttendanceLogId);

				return (maxId + 1).ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في الحصول على رقم البصمة التالي");
				return "1"; // في حالة الخطأ، يرجع 1
			}
		}


		// ✅ الحصول على أرقام البصمة غير المسجلة
		public async Task<List<string>> GetUnregisteredBiometricIdsAsync()
		{
			try
			{
				// أرقام البصمة من جدول الموظفين
				var employeeBiometricIds = await _repository.GetAllAsync()
					.ContinueWith(task => task.Result
						.Select(e => e.BiometricUserId)
						.ToList());

				// أرقام البصمة من جدول سجلات الحضور
				var attendanceBiometricIds = await _context.AttendanceLogs
					.Select(a => a.BiometricUserId)
					.Distinct()
					.ToListAsync();

				// الأرقام الموجودة في AttendanceLogs وليست في Employees
				var unregistered = attendanceBiometricIds
					.Except(employeeBiometricIds)
					.OrderBy(id => int.TryParse(id, out int num) ? num : int.MaxValue)
					.ToList();

				return unregistered;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في الحصول على أرقام البصمة غير المسجلة");
				return new List<string>();
			}
		}

		public async Task<Employee> CreateEmployeeAsync(Employee employee)
		{
			try
			{
				// التحقق من عدم تكرار BiometricUserId
				if (await _repository.BiometricIdExistsAsync(employee.BiometricUserId))
				{
					throw new InvalidOperationException(
						$"رقم الموظف '{employee.BiometricUserId}' موجود مسبقاً");
				}

				employee.IsActive = true;
				employee.CreatedDate = DateTime.Now;

				var result = await _repository.AddAsync(employee);
				_logger.LogInformation("تم إنشاء موظف جديد: {EmployeeName}", employee.EmployeeName);

				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في إنشاء موظف جديد: {EmployeeName}", employee.EmployeeName);
				throw;
			}
		}

		public async Task<Employee> UpdateEmployeeAsync(Employee employee)
		{
			try
			{
				if (!await _repository.ExistsAsync(employee.EmployeeId))
				{
					throw new InvalidOperationException("الموظف غير موجود");
				}

				// التحقق من عدم تكرار BiometricUserId
				if (await _repository.BiometricIdExistsAsync(employee.BiometricUserId, employee.EmployeeId))
				{
					throw new InvalidOperationException(
						$"رقم الموظف '{employee.BiometricUserId}' موجود مسبقاً");
				}

				employee.ModifiedDate = DateTime.Now;
				var result = await _repository.UpdateAsync(employee);

				_logger.LogInformation("تم تحديث الموظف: {EmployeeName}", employee.EmployeeName);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في تحديث الموظف {EmployeeId}", employee.EmployeeId);
				throw;
			}
		}

		public async Task DeleteEmployeeAsync(int employeeId)
		{
			try
			{
				await _repository.SoftDeleteAsync(employeeId);
				_logger.LogInformation("تم حذف الموظف {EmployeeId}", employeeId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في حذف الموظف {EmployeeId}", employeeId);
				throw;
			}
		}

		public async Task<bool> IsBiometricIdExistsAsync(string biometricUserId, int? excludeEmployeeId = null)
		{
			try
			{
				return await _repository.BiometricIdExistsAsync(biometricUserId, excludeEmployeeId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في التحقق من BiometricId: {BiometricId}", biometricUserId);
				throw;
			}
		}

		public async Task<Dictionary<string, Employee>> GetEmployeesDictionaryAsync(List<string> biometricUserIds)
		{
			try
			{
				return await _repository.GetEmployeesDictionaryAsync(biometricUserIds);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في جلب قاموس الموظفين");
				throw;
			}
		}

		public async Task<int> GetActiveEmployeeCountAsync()
		{
			try
			{
				return await _repository.GetActiveEmployeeCountAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في جلب عدد الموظفين النشطين");
				throw;
			}
		}
		// ✅ الحصول على آخر X أرقام بصمة
		public async Task<List<string>> GetLastBiometricUserIdsAsync(int count = 10)
		{
			try
			{
				// جمع الأرقام من الموظفين وسجلات الحضور
				var employeeIds = await _repository.GetAllAsync()
					.ContinueWith(task => task.Result
						.Select(e => e.BiometricUserId)
						.Where(id => int.TryParse(id, out _))
						.Select(id => int.Parse(id))
						.ToList());

				var attendanceIds = await _context.AttendanceLogs
					.Select(a => a.BiometricUserId)
					.Distinct()
					.ToListAsync();

				var attendanceIdsInt = attendanceIds
					.Where(id => int.TryParse(id, out _))
					.Select(id => int.Parse(id))
					.ToList();

				// دمج الأرقام وإزالة التكرار وترتيبها تنازلياً
				var allIds = employeeIds
					.Union(attendanceIdsInt)
					.OrderByDescending(id => id)
					.Take(count)
					.Select(id => id.ToString())
					.ToList();

				return allIds;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطأ في الحصول على آخر أرقام البصمة");
				return new List<string>();
			}
		}

	}
}
