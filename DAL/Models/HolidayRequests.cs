using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class HolidayRequests : BaseEntity
    {
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public DateTime HolidayDate { get; set; }
        public string? status { get; set; }
        public string ReasonOfHoliday { get; set; }
        public string? ReasonOfRejection { get; set; }
    }
}
