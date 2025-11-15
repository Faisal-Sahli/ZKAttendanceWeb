using System.ComponentModel.DataAnnotations;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.ViewModels
{
    public class EmployeeFormViewModel
    {
        // ══════════════════════════════════════════════════════════════
        // معلومات الموظف الأساسية
        // ══════════════════════════════════════════════════════════════
        public int EmployeeId { get; set; }

            [Required(ErrorMessage = "رقم البصمة مطلوب")]
            [Display(Name = "رقم البصمة")]
        public string BiometricUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم الموظف مطلوب")]
        [Display(Name = "اسم الموظف")]
        [StringLength(100, ErrorMessage = "اسم الموظف يجب ألا يتجاوز 100 حرف")]
        public string EmployeeName { get; set; } = string.Empty;

        [Display(Name = "الرقم الوطني")]
        [StringLength(20)]
        public string? SSN { get; set; }

        [Display(Name = "الجنس")]
        [StringLength(10)]
        public string? Gender { get; set; }

        [Display(Name = "المسمى الوظيفي")]
        [StringLength(50)]
        public string? Title { get; set; }

        [Display(Name = "رقم الهاتف")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "تاريخ التعيين")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        // ══════════════════════════════════════════════════════════════
        // العلاقات
        // ══════════════════════════════════════════════════════════════
        [Display(Name = "القسم")]
        public int? DepartmentId { get; set; }

        [Display(Name = "الوردية الافتراضية")]
        public int? DefaultShiftId { get; set; }

        // ══════════════════════════════════════════════════════════════
        // إعدادات الحضور
        // ══════════════════════════════════════════════════════════════
        [Display(Name = "مراقبة الحضور")]
        public bool CheckAttendance { get; set; } = true;

        [Display(Name = "احتساب التأخير")]
        public bool CheckLate { get; set; } = true;

        [Display(Name = "احتساب الانصراف المبكر")]
        public bool CheckEarly { get; set; } = true;

        [Display(Name = "احتساب الوقت الإضافي")]
        public bool CheckOvertime { get; set; } = true;

        [Display(Name = "احتساب الإجازات")]
        public bool CheckHoliday { get; set; } = true;

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        // ══════════════════════════════════════════════════════════════
        // ✅ NEW: الفروع والأجهزة
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// الفروع مجموعة حسب المدينة مع أجهزتها
        /// </summary>
        public Dictionary<string, List<BranchDeviceViewModel>> BranchesByCity { get; set; } = new();

        /// <summary>
        /// IDs الأجهزة المحددة (للحفظ/التحديث)
        /// </summary>
        public List<int> SelectedDeviceIds { get; set; } = new();

        /// <summary>
        /// IDs الفروع المحددة (optional - للعرض فقط)
        /// </summary>
        public List<int> SelectedBranchIds { get; set; } = new();

        // ══════════════════════════════════════════════════════════════
        // ✅ OLD: للتوافق مع الكود القديم (إذا موجود)
        // ══════════════════════════════════════════════════════════════
        public List<BranchCheckboxItem>? AvailableBranches { get; set; }
        public bool SelectAllBranches { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    // ✅ Helper Class (للتوافق مع الكود القديم)
    // ══════════════════════════════════════════════════════════════
    public class BranchCheckboxItem
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}