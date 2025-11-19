using CMCS1.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS1.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Lecturer name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Lecturer name must be between 3 and 100 characters.")]
        [Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hours worked is required.")]
        [Range(1, 200, ErrorMessage = "Hours worked must be between 1 and 200 hours.")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required.")]
        [Range(100, 2000, ErrorMessage = "Hourly rate must be between R100 and R2000.")]
        [Display(Name = "Hourly Rate (R)")]
        public decimal HourlyRate { get; set; }

        [NotMapped]
        [Display(Name = "Total (R)")]
        public decimal TotalAmount => Math.Round(HoursWorked * HourlyRate, 2);

        [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters.")]
        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Submitted On")]
        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        // Stores filename of uploaded supporting document
        public string? UploadedFileName { get; set; }
    }
}


