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
    }
}
