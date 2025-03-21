using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Specification
{
    public class EmployeeSpec : BaseSpecification<Employee>
    {
        public EmployeeSpec() : base() { }

        public EmployeeSpec(int id, int? month = null, int? year = null) : base(e => e.Id == id)
        {
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            AddFilter(e => e.MonthlyData.Any(m => m.Month == currentMonth && m.Year == currentYear));

            AddInclude(e => e.MonthlyData);
        }

        public EmployeeSpec(int? month = null, int? year = null, string? branchName = null)
        {
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            Criteria = e => e.MonthlyData.Any(m =>
                (month == null || m.Month == currentMonth) &&
                (year == null || m.Year == currentYear)
            );

            if (!string.IsNullOrEmpty(branchName))
            {
                AddFilter(e => e.BranchName.Contains(branchName));
            }

            AddInclude(e => e.MonthlyData);
        }
    }
}
