using System.ComponentModel.DataAnnotations;

namespace HRManagement.ViewModel
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }
        public int MonthlyEmployeeDataId { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string BranchName { get; set; }

        [Required]
        public string Role { get; set; }

        public string? BankName { get; set; }
        public string? BankAccount { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        public double? Hours { get; set; }
        public double? HoursOverTime { get; set; }
        public double? ForgetedHours { get; set; }
        public double? SalaryPerHour { get; set; }
        public double? TotalSalary { get; set; }
        public int? Holidaies { get; set; }

        public double? NetSalary { get; set; }

    }

}
