using System.ComponentModel.DataAnnotations;

namespace CMCS1.Models
{
    public class EditLecturerViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
    }
}
