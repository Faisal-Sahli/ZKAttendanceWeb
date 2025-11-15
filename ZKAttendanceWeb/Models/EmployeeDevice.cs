using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKAttendanceWeb.Models
{
    [Table("EmployeeDevices")]
    public class EmployeeDevice
    {
        [Key]
        public int EmployeeDeviceId { get; set; }

       
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public virtual Device? Device { get; set; }
    }
}