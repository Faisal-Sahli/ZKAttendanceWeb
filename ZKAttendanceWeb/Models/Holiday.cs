using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKAttendanceWeb.Models
{
    [Table("Holidays")]
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        [Required]
        [StringLength(100)]
        public string HolidayName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "date")]
        public DateTime HolidayDate { get; set; }

        public int DurationDays { get; set; } = 1;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? HolidayType { get; set; }

        // ✅ جديد: للعطل المتكررة سنوياً
        public bool IsRecurring { get; set; } = false;

        // ✅ جديد: للراحة الأسبوعية المتكررة
        public bool IsWeeklyRecurring { get; set; } = false;

        // ✅ جديد: يوم الأسبوع للعطل الأسبوعية (0=Sunday, 5=Friday)
        [Column(TypeName = "int")]
        public DayOfWeek? RecurringDayOfWeek { get; set; }

        // ✅ جديد: تطبيق على فرع محدد أو الكل (null = الكل)
        public int? BranchId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }

}
