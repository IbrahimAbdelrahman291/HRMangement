using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class MonthlyEmployeeData : BaseEntity
    {
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public int Month { get; set; } 
        public int Year { get; set; } 

        public double? Hours { get; set; }
        public double? HoursOverTime { get; set; }
        public double? ForgetedHours { get; set; }
        public double? SalaryPerHour { get; set; }
        public double? TotalSalary { get; set; }
        public int? Holidaies { get; set; }
        public double? NetSalary { get; set; }

        public ICollection<Discounts> Discounts { get; set; } = new List<Discounts>();
        public ICollection<Bouns> Bounss { get; set; } = new List<Bouns>();
        public ICollection<Borrow> Borrows { get; set; } = new List<Borrow>();
    }
}
