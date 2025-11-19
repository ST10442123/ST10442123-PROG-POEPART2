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
    }
}
