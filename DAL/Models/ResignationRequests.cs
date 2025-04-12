using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class ResignationRequests : BaseEntity
    {
        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public DateTime ResignationDate { get; set; }
        public string? status { get; set; }
        public string ReasonOfResignation { get; set; }
        public string? ReasonOfRejection { get; set; }
    }
}
