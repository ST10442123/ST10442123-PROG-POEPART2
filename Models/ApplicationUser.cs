using Microsoft.AspNetCore.Identity;

namespace CMCS1.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Keep null to avoid errors
        public string? FullName { get; set; }
    }
}

