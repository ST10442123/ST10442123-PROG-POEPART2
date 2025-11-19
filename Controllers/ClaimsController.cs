
using CMCS1.Data;
using CMCS1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS1.Controllers
{
    [Authorize] // login required for all claim pages
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ClaimsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Test() => Content("ClaimsController is working!");

        // Submit (GET) — everyone logged-in
        [HttpGet]
        public IActionResult Submit() => View(new Claim());

        // Submit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Claim model, IFormFile? uploadedFile)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    var uploads = Path.Combine(_env.WebRootPath, "uploads");
                    Directory.CreateDirectory(uploads);
                    var unique = $"{Guid.NewGuid()}{Path.GetExtension(uploadedFile.FileName)}";
                    using var fs = System.IO.File.Create(Path.Combine(uploads, unique));
                    await uploadedFile.CopyToAsync(fs);
                    model.UploadedFileName = unique;
                }

                model.DateSubmitted = DateTime.Now;
                model.Status = ClaimStatus.Pending;

                _context.Claims.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "✅ Claim submitted successfully!";
                return RedirectToAction(nameof(Submit));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                TempData["ErrorMessage"] = "❌ Error submitting claim. Please try again.";
                return View(model);
            }
        }

        // Track — everyone logged-in
        [HttpGet]
        public IActionResult Track()
        {
            var list = _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToList();
            return View(list);
        }

        // Review — view only, everyone logged-in
        [HttpGet]
        public IActionResult Review(string? searchLecturer, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Claims
                .Where(c => c.Status == ClaimStatus.Pending);

            // Filter by lecturer name (contains)
            if (!string.IsNullOrWhiteSpace(searchLecturer))
            {
                var term = searchLecturer.Trim();
                query = query.Where(c => c.LecturerName.Contains(term));
            }

            // Filter by date range (DateSubmitted)
            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(c => c.DateSubmitted >= from);
            }

            if (toDate.HasValue)
            {
                // include the whole "to" day
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(c => c.DateSubmitted <= to);
            }

            var pending = query
                .OrderByDescending(c => c.DateSubmitted)
                .ToList();

            return View(pending);
        }


        // Approve — only Coordinator & Manager
        [HttpPost]
        [Authorize(Roles = "Coordinator,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            try
            {
                var claim = _context.Claims.Find(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Review));
                }

                // 🔄 Automated policy checks before approval
                if (!IsClaimWithinPolicy(claim, out var reason))
                {
                    claim.Status = ClaimStatus.Rejected;
                    _context.SaveChanges();
                    TempData["ErrorMessage"] = $"Claim automatically rejected: {reason}";
                    return RedirectToAction(nameof(Review));
                }

                claim.Status = ClaimStatus.Approved;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "✅ Claim approved.";
                return RedirectToAction(nameof(Review));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                TempData["ErrorMessage"] = "❌ Error approving claim.";
                return RedirectToAction(nameof(Review));
            }
        }



        // Reject — only Coordinator & Manager
        [HttpPost]
        [Authorize(Roles = "Coordinator,Manager")]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            try
            {
                var claim = _context.Claims.Find(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Review));
                }
                claim.Status = ClaimStatus.Rejected;
                _context.SaveChanges();
                TempData["SuccessMessage"] = "✅ Claim rejected.";
                return RedirectToAction(nameof(Review));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                TempData["ErrorMessage"] = "❌ Error rejecting claim.";
                return RedirectToAction(nameof(Review));
            }
        }

        // All claims (optional listing)
        [HttpGet]
        public IActionResult Index()
        {
            var claims = _context.Claims
                .OrderByDescending(c => c.DateSubmitted)
                .ToList();
            return View(claims);
        }


        /// <summary>
        /// Automatic business rule checks for claim approval.
        /// Adjust the rules here if policies change.
        /// </summary>
        private bool IsClaimWithinPolicy(Claim claim, out string reason)
        {
            // Example policy: hours between 1 and 200
            if (claim.HoursWorked < 1 || claim.HoursWorked > 200)
            {
                reason = "Hours worked must be between 1 and 200 for approval.";
                return false;
            }

            // Example policy: hourly rate between R100 and R2000
            if (claim.HourlyRate < 100 || claim.HourlyRate > 2000)
            {
                reason = "Hourly rate must be between R100 and R2000 for approval.";
                return false;
            }

            // You can add more automatic rules here if needed
            reason = string.Empty;
            return true;
        }

    }
}

