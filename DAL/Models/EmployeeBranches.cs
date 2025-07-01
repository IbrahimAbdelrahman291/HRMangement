using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class EmployeeBranches : BaseEntity
    {
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public string BranchName { get; set; }

        public DateTime StartDate { get; set; }

    }
}
