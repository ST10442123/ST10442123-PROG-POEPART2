using CMCS1.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS1.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [Required, Display(Name = "Lecturer Name")]
        public string LecturerName { get; set; } = string.Empty;

        [Range(0, 1000)]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Range(0, 100000)]
        [Display(Name = "Hourly Rate (R)")]
        public decimal HourlyRate { get; set; }

        [NotMapped]
        [Display(Name = "Total (R)")]
        public decimal TotalAmount => Math.Round(HoursWorked * HourlyRate, 2);

        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        public DateTime DateSubmitted { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        // to store filename of uploaded doc
        public string? UploadedFileName { get; set; }
    }
}

