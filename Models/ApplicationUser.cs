using Microsoft.AspNetCore.Identity;

namespace CMCS1.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Keep nullable to avoid registration errors
        public string? FullName { get; set; }
    }
}

