using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Models;

namespace ZKAttendanceWeb.Data
{
    public class ZKAttendanceWebDbContext : DbContext
    {
        public ZKAttendanceWebDbContext(DbContextOptions<ZKAttendanceWebDbContext> options)
            : base(options)
        {
        }

        // ═════════════════════════════════════════════════════════════════
        // DbSets - مرتبة حسب الأهمية
        // ═════════════════════════════════════════════════════════════════

        // Core Tables
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<Holiday> Holidays { get; set; }

        // Attendance Tables
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }

        // System Tables
        public DbSet<SyncLog> SyncLogs { get; set; }
        public DbSet<DeviceStatus> DeviceStatuses { get; set; }
        public DbSet<DeviceError> DeviceErrors { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<EmployeeShiftAssignment> EmployeeShiftAssignments { get; set; }

        // ✅ NEW: Many-to-Many Relationship Table
        public DbSet<EmployeeBranch> EmployeeBranches { get; set; }
        public DbSet<EmployeeDevice> EmployeeDevices { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═════════════════════════════════════════════════════
            // Branches Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<Branch>(entity =>
            {
                entity.HasKey(e => e.BranchId);
                entity.Property(e => e.BranchId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.BranchCode)
                      .IsUnique()
                      .HasDatabaseName("IX_Branch_Code");
            });

            // ═════════════════════════════════════════════════════
            // Devices Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<Device>(entity =>
            {
                entity.HasKey(e => e.DeviceId);
                entity.Property(e => e.DeviceId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Branch)
                      .WithMany(b => b.Devices)
                      .HasForeignKey(d => d.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.DeviceIP, e.DevicePort })
                      .IsUnique()
                      .HasDatabaseName("IX_Device_IP_Port");
            });

            // ═════════════════════════════════════════════════════
            // Departments Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.DepartmentId);
                entity.Property(e => e.DepartmentId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                // Self-referencing relationship
                entity.HasOne(d => d.ParentDepartment)
                      .WithMany(d => d.SubDepartments)
                      .HasForeignKey(d => d.ParentDepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.DepartmentName)
                      .IsUnique()
                      .HasDatabaseName("IX_Department_Name");
            });

            // ═════════════════════════════════════════════════════
            // WorkShifts Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<WorkShift>(entity =>
            {
                entity.HasKey(e => e.ShiftId);
                entity.Property(e => e.ShiftId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.ShiftName)
                      .HasDatabaseName("IX_WorkShift_Name");
            });

            // ═════════════════════════════════════════════════════
            // Holidays Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<Holiday>(entity =>
            {
                entity.HasKey(e => e.HolidayId);
                entity.Property(e => e.HolidayId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => new { e.HolidayName, e.HolidayDate })
                      .IsUnique()
                      .HasDatabaseName("IX_Holiday_NameDate");
            });

            // ═════════════════════════════════════════════════════
            // Employees Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);
                entity.Property(e => e.EmployeeId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DefaultShift)
                      .WithMany(s => s.Employees)
                      .HasForeignKey(e => e.DefaultShiftId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.BiometricUserId)
                      .IsUnique()
                      .HasDatabaseName("IX_Employee_BiometricUserId");
            });

            // ═════════════════════════════════════════════════════
            // AttendanceLogs Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<AttendanceLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsSynced).HasDefaultValue(true);
                entity.Property(e => e.IsProcessed).HasDefaultValue(false);
                entity.Property(e => e.IsManual).HasDefaultValue(false);

                entity.HasOne(a => a.Device)
                      .WithMany(d => d.AttendanceLogs)
                      .HasForeignKey(a => a.DeviceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Branch)
                      .WithMany(b => b.AttendanceLogs)
                      .HasForeignKey(a => a.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Employee)
                      .WithMany(e => e.AttendanceLogs)
                      .HasForeignKey(a => a.EmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.BiometricUserId, e.AttendanceTime, e.DeviceId })
                      .IsUnique()
                      .HasDatabaseName("IX_AttendanceLog_Unique");

                entity.HasIndex(e => e.IsSynced)
                      .HasDatabaseName("IX_AttendanceLog_IsSynced");

                entity.HasIndex(e => e.IsProcessed)
                      .HasDatabaseName("IX_AttendanceLog_IsProcessed");
            });

            // ═════════════════════════════════════════════════════
            // SyncLogs Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<SyncLog>(entity =>
            {
                entity.HasKey(e => e.SyncId);
                entity.Property(e => e.SyncId).ValueGeneratedOnAdd();
                entity.Property(e => e.StartTime).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Status).HasDefaultValue("Pending");

                entity.HasOne(s => s.Device)
                      .WithMany()
                      .HasForeignKey(s => s.DeviceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Branch)
                      .WithMany()
                      .HasForeignKey(s => s.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.DeviceId, e.StartTime })
                      .HasDatabaseName("IX_SyncLog_Device_Time");

                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_SyncLog_Status");
            });

            // ═════════════════════════════════════════════════════
            // DeviceStatus Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<DeviceStatus>(entity =>
            {
                entity.HasKey(e => e.StatusId);
                entity.Property(e => e.StatusId).ValueGeneratedOnAdd();
                entity.Property(e => e.StatusTime).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.LastUpdateTime).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsOnline).HasDefaultValue(false);

                entity.HasOne(ds => ds.Device)
                      .WithMany(d => d.DeviceStatuses)
                      .HasForeignKey(ds => ds.DeviceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ds => ds.Branch)
                      .WithMany()
                      .HasForeignKey(ds => ds.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.DeviceId, e.StatusTime })
                      .HasDatabaseName("IX_DeviceStatus_Device_Time");

                entity.HasIndex(e => e.IsOnline)
                      .HasDatabaseName("IX_DeviceStatus_IsOnline");
            });

            // ═════════════════════════════════════════════════════
            // DeviceErrors Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<DeviceError>(entity =>
            {
                entity.HasKey(e => e.ErrorId);
                entity.Property(e => e.ErrorId).ValueGeneratedOnAdd();
                entity.Property(e => e.ErrorDateTime).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Severity).HasDefaultValue("Medium");
                entity.Property(e => e.IsResolved).HasDefaultValue(false);

                entity.HasOne(de => de.Device)
                      .WithMany()
                      .HasForeignKey(de => de.DeviceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(de => de.Branch)
                      .WithMany()
                      .HasForeignKey(de => de.BranchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.DeviceId, e.ErrorDateTime })
                      .HasDatabaseName("IX_DeviceError_Device_Time");

                entity.HasIndex(e => e.IsResolved)
                      .HasDatabaseName("IX_DeviceError_IsResolved");
            });

            // ═════════════════════════════════════════════════════
            // SystemSettings Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.HasKey(e => e.SettingId);
                entity.Property(e => e.SettingId).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.SettingKey)
                      .IsUnique()
                      .HasDatabaseName("IX_SystemSetting_Key");
            });

            // ═════════════════════════════════════════════════════
            // EmployeeShiftAssignments Configuration
            // ═════════════════════════════════════════════════════
            modelBuilder.Entity<EmployeeShiftAssignment>(entity =>
            {
                entity.HasKey(e => e.AssignmentId);
                entity.Property(e => e.AssignmentId).ValueGeneratedOnAdd();
                entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Shift)
                      .WithMany()
                      .HasForeignKey(e => e.ShiftId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.EmployeeId, e.EffectiveFrom })
                      .HasDatabaseName("IX_EmployeeShift_Effective");
            });

            // ═════════════════════════════════════════════════════════════════
            // ✅ NEW: EmployeeBranches Configuration (Many-to-Many)
            // ═════════════════════════════════════════════════════════════════
            modelBuilder.Entity<EmployeeBranch>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.EmployeeBranchId);
                entity.Property(e => e.EmployeeBranchId)
                    .ValueGeneratedOnAdd();

                // Relationship: Employee → EmployeeBranches
                entity.HasOne(eb => eb.Employee)
                    .WithMany(e => e.EmployeeBranches)
                    .HasForeignKey(eb => eb.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade); // حذف الموظف = حذف كل سجلاته

                // Relationship: Branch → EmployeeBranches
                entity.HasOne(eb => eb.Branch)
                    .WithMany(b => b.EmployeeBranches)
                    .HasForeignKey(eb => eb.BranchId)
                    .OnDelete(DeleteBehavior.Restrict); // منع حذف فرع فيه موظفين

                // Unique Index: موظف واحد لا يتكرر في نفس الفرع
                entity.HasIndex(e => new { e.EmployeeId, e.BranchId })
                    .IsUnique()
                    .HasDatabaseName("IX_EmployeeBranch_Unique");

                // Default Values
                entity.Property(e => e.AssignedDate)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
            });

            // ═════════════════════════════════════════════════════════════════
            // ✅ NEW: EmployeeDevice Configuration (Many-to-Many)
            // ═════════════════════════════════════════════════════════════════
            modelBuilder.Entity<EmployeeDevice>(entity =>
            {
                entity.HasKey(e => e.EmployeeDeviceId);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.EmployeeDevices)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Device)
                    .WithMany(d => d.EmployeeDevices)
                    .HasForeignKey(e => e.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Prevent duplicate Employee-Device assignments
                entity.HasIndex(e => new { e.EmployeeId, e.DeviceId })
                    .IsUnique();
            });
        }
    }
}
