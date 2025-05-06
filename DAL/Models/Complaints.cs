using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class Complaints : BaseEntity
    {
        public string content { get; set; }
        public string status { get; set; }
        public DateTime Date { get; set; }
        public string? response { get; set; }
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }


    }
}
