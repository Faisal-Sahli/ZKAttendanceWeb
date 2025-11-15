using Microsoft.EntityFrameworkCore;
using ZKAttendanceWeb.Data;
using ZKAttendanceWeb.Data.Repositories;
using ZKAttendanceWeb.Repostries;
using ZKAttendanceWeb.Services.Attendance;
using ZKAttendanceWeb.Services.Attendances;
using ZKAttendanceWeb.Services.Branches;      // ✅ محدّث
using ZKAttendanceWeb.Services.Common;
using ZKAttendanceWeb.Services.Departments;   // ✅ محدّث
using ZKAttendanceWeb.Services.Devices;       // ✅ محدّث
using ZKAttendanceWeb.Services.Employees;     // ✅ محدّث
using ZKAttendanceWeb.Services.Report;
using ZKAttendanceWeb.Services.Shifts;
using ZKAttendanceWeb.Services.WorkShifts;      



var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════
// MVC + JSON Settings
// ═══════════════════════════════════════════════════════
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// ═══════════════════════════════════════════════════════
// Database Configuration
// ═══════════════════════════════════════════════════════
builder.Services.AddDbContext<ZKAttendanceWebDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ═══════════════════════════════════════════════════════
// Memory Cache
// ═══════════════════════════════════════════════════════
builder.Services.AddMemoryCache();

// ═══════════════════════════════════════════════════════
// Repositories
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<BranchRepository>();
builder.Services.AddScoped<DeviceRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<DepartmentRepository>();
builder.Services.AddScoped<WorkShiftRepository>();
builder.Services.AddScoped<AttendanceLogRepository>();

// ═══════════════════════════════════════════════════════
// Core Services
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<IBrancheService, BrancheService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IWorkShiftService, WorkShiftService>();

// ═══════════════════════════════════════════════════════
// Attendance Services
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<AttendanceCalculationService>();
builder.Services.AddScoped<AttendanceQueryService>();

// ═══════════════════════════════════════════════════════
// Shift Services
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<ShiftAssignmentService>();

// ═══════════════════════════════════════════════════════
// Report Services
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ExcelReportService>();
builder.Services.AddScoped<PdfReportService>();

// ═══════════════════════════════════════════════════════
// Common Services
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<LookupService>();

// ═══════════════════════════════════════════════════════
// Background Services
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<IDeviceMonitorService, DeviceMonitorService>();
builder.Services.AddHostedService<DeviceMonitorBackgroundService>();

// ═══════════════════════════════════════════════════════
// API Documentation (Swagger)
// ═══════════════════════════════════════════════════════
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ═══════════════════════════════════════════════════════
// Build Application
// ═══════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════
// Configure HTTP Pipeline
// ═══════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
