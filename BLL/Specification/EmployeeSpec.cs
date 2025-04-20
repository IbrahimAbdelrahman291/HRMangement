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
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;


            AddFilter(e => e.MonthlyData.Any(m => m.Month == currentMonth && m.Year == currentYear));

            AddInclude(e => e.MonthlyData);
        }

        public EmployeeSpec(int? month = null, int? year = null, string? branchName = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

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
        public EmployeeSpec(int? month = null , int? year = null, string? branchName = null , string? bankName = null , string? Role = null, string? name = null) 
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

            Criteria = e => e.MonthlyData.Any(m =>
                (month == null || m.Month == currentMonth) &&
                (year == null || m.Year == currentYear)
            );

            if (!string.IsNullOrEmpty(branchName))
            {
                AddFilter(e => e.BranchName.Contains(branchName));
            }
            if (!string.IsNullOrEmpty(bankName))
            {
                AddFilter(e => e.BankName.Contains(bankName));
            }
            if (!string.IsNullOrEmpty(Role))
            {
                AddFilter(e => e.Role.Contains(Role));
            }
            if (!string.IsNullOrEmpty(name))
            {
                AddFilter(e => e.Name.Contains(name));
            }
            AddInclude(e => e.MonthlyData);
        }
    }
}
