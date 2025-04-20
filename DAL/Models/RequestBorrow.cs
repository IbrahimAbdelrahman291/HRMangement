using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class RequestBorrow : BaseEntity
    {
        public int Amount { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? notes { get; set; }
        public string? ReasonOfRejection { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
    }
}
