using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Specification
{
    public class WorkLogsSpec : BaseSpecification<WorkLogs>
    {
        public WorkLogsSpec() : base() { }

        public WorkLogsSpec(DateOnly? date) 
        {
            AddFilter(w => w.Day == date);
            AddInclude(w => w.Employee);
        }
        public WorkLogsSpec(DateTime? StartDate = null, DateTime? EndDate = null, int? EmployeeId = null, string? EmployeeName = null, string? BranchName = null) : base()
        {
            if (StartDate.HasValue)
            {
                var startDateOnly = DateOnly.FromDateTime(StartDate.Value.Date);
                AddFilter(w => w.Day >= startDateOnly);
            }

            if (EndDate.HasValue)
            {
                var endDateOnly = DateOnly.FromDateTime(EndDate.Value.Date);
                AddFilter(w => w.Day <= endDateOnly);
            }

            if (EmployeeId.HasValue)
            {
                AddFilter(w => w.EmployeeId == EmployeeId.Value);
            }

            if (!string.IsNullOrEmpty(EmployeeName))
            {
                AddFilter(w => w.Employee.Name.Contains(EmployeeName));
            }

            if (!string.IsNullOrEmpty(BranchName))
            {
                AddFilter(w => w.Employee.BranchName == BranchName);
            }

            AddInclude(w => w.Employee);
        }
    }
}
