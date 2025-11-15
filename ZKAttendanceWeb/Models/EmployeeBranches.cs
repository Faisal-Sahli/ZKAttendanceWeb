// Models/EmployeeBranch.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKAttendanceWeb.Models
{
    /// <summary>
    /// جدول وسيط لربط الموظفين بالفروع (Many-to-Many Relationship)
    /// يسمح للموظف الواحد بالعمل في عدة فروع
    /// </summary>
    [Table("EmployeeBranches")]
    public class EmployeeBranch
    {
        /// <summary>
        /// المفتاح الأساسي
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeBranchId { get; set; }

        /// <summary>
        /// معرّف الموظف (Foreign Key)
        /// </summary>
        [Required(ErrorMessage = "معرّف الموظف مطلوب")]
        public int EmployeeId { get; set; }

        /// <summary>
        /// معرّف الفرع (Foreign Key)
        /// </summary>
        [Required(ErrorMessage = "معرّف الفرع مطلوب")]
        public int BranchId { get; set; }

        /// <summary>
        /// تاريخ تعيين الموظف في الفرع
        /// </summary>
        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// هل التعيين نشط؟
        /// false = الموظف لم يعد يعمل في هذا الفرع
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاريخ إيقاف التعيين (إن وُجد)
        /// </summary>
        public DateTime? DeactivatedDate { get; set; }

        /// <summary>
        /// ملاحظات إضافية (اختياري)
        /// مثال: "منقول من فرع الرياض"
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // ═════════════════════════════════════════════════════════════
        // Navigation Properties
        // ═════════════════════════════════════════════════════════════

        /// <summary>
        /// بيانات الموظف
        /// </summary>
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; } = null!;

        /// <summary>
        /// بيانات الفرع
        /// </summary>
        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; } = null!;
    }
}
