using System.Text;
using System.Linq;
using CMCS1.Data;
using CMCS1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMCS1.Controllers
{
    /// <summary>
    /// HR area for the CMCS system.
    /// Provides an automated dashboard for approved claims,
    /// monthly reporting with CSV export, and basic lecturer management.
    /// </summary>

    // HR dashboard and lecturer management - restricted to Manager (HR) role
    [Authorize(Roles = "Manager")]
    public class HrController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HrController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        /// <summary>
        /// HR dashboard showing all approved claims and summary totals.
        /// This supports the HR automation requirement from Part 3 of the POE.
        /// </summary>
        // HR Dashboard - all approved claims
        [HttpGet]
        public IActionResult Index()
        {
            var approvedClaims = _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved)
                .OrderByDescending(c => c.DateSubmitted)
                .ToList();

            return View(approvedClaims);
        }



        /// <summary>
        /// Generates a monthly view of approved claims for a selected year and month.
        /// Used for HR reporting and to support payroll processing.
        /// </summary>
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




        /// <summary>
        /// Exports the monthly approved claims to a CSV file so HR can import
        /// the data into external systems (e.g., Excel / payroll).
        /// </summary>
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





        /// <summary>
        /// Lists all users in the Lecturer role with their basic contact details.
        /// Enables HR to view who will be paid for approved claims.
        /// </summary>
        // List lecturers and their basic details (for HR)
        [HttpGet]
        public async Task<IActionResult> Lecturers()
        {
            var users = _userManager.Users.ToList();
            var result = new List<LecturerViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Only list lecturers; remove this if HR must see everyone
                if (!roles.Contains("Lecturer"))
                {
                    continue;
                }

                result.Add(new LecturerViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName ?? user.UserName ?? string.Empty,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Roles = string.Join(", ", roles)
                });
            }

            return View(result);
        }





        /// <summary>
        /// Loads the edit form for a specific lecturer.
        /// </summary>
        // Edit lecturer details (GET)
        [HttpGet]
        public async Task<IActionResult> EditLecturer(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var vm = new EditLecturerViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(vm);
        }



        /// <summary>
        /// Saves basic lecturer profile changes (FullName, PhoneNumber).
        /// Keeps login details unchanged to avoid breaking authentication.
        /// </summary>
        // Edit lecturer details (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(EditLecturerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Lecturer not found.";
                return RedirectToAction(nameof(Lecturers));
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            // We keep Email/UserName as-is to avoid breaking login.
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Lecturer details updated successfully.";
                return RedirectToAction(nameof(Lecturers));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

    }
}


