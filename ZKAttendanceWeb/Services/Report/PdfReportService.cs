using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZKAttendanceWeb.Models;
using ZKAttendanceWeb.ViewModels;

namespace ZKAttendanceWeb.Services.Report
{
    public class PdfReportService
    {
        public byte[] GeneratePdfReport(List<AttendanceViewModel> data, DateTime? fromDate, DateTime? toDate)
        {
            var groupedByEmployee = data
                .GroupBy(d => d.BiometricUserId)
                .Select(g => new
                {
                    BiometricUserId = g.Key,
                    EmployeeName = g.First().EmployeeName,
                    BranchName = g.First().BranchName,
                    Records = g.OrderBy(x => x.Date).ToList(),
                    TotalHours = g.Sum(x => x.WorkingHours),
                    PresentDays = g.Count(x => x.Status == "حضور كامل"),
                    PartialDays = g.Count(x => x.Status == "دخول فقط"),
                    AbsentDays = g.Count(x => x.Status == "غياب")
                })
                .ToList();

            var document = Document.Create(container =>
            {
                foreach (var employee in groupedByEmployee)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(15);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                        page.Header().Column(column =>
                        {
                            column.Item().Background(Colors.Blue.Darken2).Padding(8)
                                .AlignCenter().Text("تقرير الحضور والانصراف")
                                .FontSize(14).Bold().FontColor(Colors.White);

                            column.Item().PaddingTop(3).AlignCenter()
                                .Text($"من {fromDate?.ToString("dd/MM/yyyy") ?? "البداية"} إلى {toDate?.ToString("dd/MM/yyyy") ?? "النهاية"}")
                                .FontSize(10);
                        });

                        page.Content().PaddingTop(10).Column(column =>
                        {
                            column.Item().Text($"الموظف: {employee.EmployeeName}  |  رقم البصمة: {employee.BiometricUserId}")
                                .FontSize(11).Bold();

                            column.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(30);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("#").Bold();
                                    header.Cell().Text("التاريخ").Bold();
                                    header.Cell().Text("الدخول").Bold();
                                    header.Cell().Text("الخروج").Bold();
                                    header.Cell().Text("ساعات").Bold();
                                    header.Cell().Text("الحالة").Bold();
                                });

                                int index = 1;
                                foreach (var record in employee.Records)
                                {
                                    table.Cell().Text(index.ToString());
                                    table.Cell().Text(record.Date.ToString("dd/MM/yyyy"));
                                    table.Cell().Text(record.CheckInTime?.ToString("HH:mm") ?? "-");
                                    table.Cell().Text(record.CheckOutTime?.ToString("HH:mm") ?? "-");
                                    table.Cell().Text(record.WorkingHours > 0
                                        ? $"{record.WorkingHours:F1}" : "-");
                                    table.Cell().Text(record.Status);
                                    index++;
                                }
                            });

                            column.Item().PaddingTop(15).Text($"إجمالي ساعات العمل: {employee.TotalHours:F2} ساعة")
                                .Bold().FontSize(11);
                        });
                    });
                }
            });

            return document.GeneratePdf();
        }
    }
}
