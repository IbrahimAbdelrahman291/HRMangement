using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class EmployeeHistory : BaseEntity
    {
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public DateTime HiringDate { get; set; }          
        public string Qualification { get; set; }          
        public int GraduationYear { get; set; }            

        public DateTime? EndOfServiceDate { get; set; }
        public string? EndOfServiceReason { get; set; }
    }
}
