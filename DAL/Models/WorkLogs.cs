using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class WorkLogs : BaseEntity
    {
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public TimeSpan TotalTime { get; set; }
        public DateOnly Day { get; set; }
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

    }
}
