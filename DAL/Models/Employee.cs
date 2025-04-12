using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Employee : BaseEntity
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string BranchName { get; set; }

        [Required]
        public string Role { get; set; }

        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string UserId { get; set; }

        public ICollection<WorkLogs> workLogs { get; set; } = new List<WorkLogs>();
        public ICollection<MonthlyEmployeeData> MonthlyData { get; set; } = new List<MonthlyEmployeeData>();
        public ICollection<HolidayRequests> HolidayRequests { get; set; } = new List<HolidayRequests>();
        public ICollection<ResignationRequests> ResignationRequests { get; set; } = new List<ResignationRequests>();
    }
}
