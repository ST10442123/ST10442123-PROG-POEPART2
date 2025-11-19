using System.Text;
using CMCS1.Data;
using CMCS1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS1.Controllers
{
    // HR dashboard - restricted to Manager for now (acts as HR)
    [Authorize(Roles = "Manager")]
    public class HrController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HrController(ApplicationDbContext context)
        {
            _context = context;
        }

        // HR Dashboard - View all approved claims
        [HttpGet]
        public IActionResult Index()
        {
            var approvedClaims = _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved)
                .OrderByDescending(c => c.DateSubmitted)
                .ToList();

            return View(approvedClaims);
        }

        // Monthly report - filter approved claims by year + month
        [HttpGet]
        public IActionResult MonthlyReport(int? year, int? month)
        {
            var today = DateTime.Today;
            var selectedYear = year ?? today.Year;
            var selectedMonth = month ?? today.Month;

            var fromDate = new DateTime(selectedYear, selectedMonth, 1);
            var toDate = fromDate.AddMonths(1).AddTicks(-1);

            var claims = _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved &&
                            c.DateSubmitted >= fromDate &&
                            c.DateSubmitted <= toDate)
                .OrderBy(c => c.DateSubmitted)
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;

            return View(claims);
        }

        // CSV export for monthly approved claims
        [HttpGet]
        public IActionResult ExportCsv(int? year, int? month)
        {
            var today = DateTime.Today;
            var selectedYear = year ?? today.Year;
            var selectedMonth = month ?? today.Month;

            var fromDate = new DateTime(selectedYear, selectedMonth, 1);
            var toDate = fromDate.AddMonths(1).AddTicks(-1);

            var claims = _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved &&
                            c.DateSubmitted >= fromDate &&
                            c.DateSubmitted <= toDate)
                .OrderBy(c => c.DateSubmitted)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Lecturer,DateSubmitted,HoursWorked,HourlyRate,TotalAmount,Notes");

            foreach (var c in claims)
            {
                var line = string.Join(",",
                    EscapeCsv(c.LecturerName),
                    c.DateSubmitted.ToString("yyyy-MM-dd"),
                    c.HoursWorked.ToString(),
                    c.HourlyRate.ToString("F2"),
                    c.TotalAmount.ToString("F2"),
                    EscapeCsv(c.Notes)
                );

                sb.AppendLine(line);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"ApprovedClaims_{selectedYear}_{selectedMonth:00}.csv";

            return File(bytes, "text/csv", fileName);
        }

        private static string EscapeCsv(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var value = input.Replace("\"", "\"\"");
            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');

            return needsQuotes ? $"\"{value}\"" : value;
        }
    }
}

