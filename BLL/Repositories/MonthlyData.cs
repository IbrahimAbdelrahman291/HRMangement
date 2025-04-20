using BLL.Interfaces;
using BLL.Specification;
using DAL.Context;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Repositories
{
    public class MonthlyData : IMonthlyData
    {
        private readonly IGenericRepository<Employee> _empRepo;
        private readonly HRDbContext _dbContext;

        public MonthlyData(IGenericRepository<Employee> empRepo, HRDbContext dbContext)
        {
            _empRepo = empRepo;
            _dbContext = dbContext;
        }
        public async Task NewMonthlyData()
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);
            var previousMonth = egyptDate.AddMonths(-1);
            var year = previousMonth.Year;
            var month = previousMonth.Month;
            string? BrachName = null;
            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(month,year,BrachName));
            var monthlyData = _dbContext.MonthlyEmployeeData.Where(x => x.Month == month && x.Year == year).ToList();
            if (employees.Count()>0 && employees != null)
            {
                if (monthlyData.Count>0 && monthlyData != null)
                {
                    foreach (var emp in employees)
                    {
                        var EmpData = monthlyData.FirstOrDefault(x => x.EmployeeId == emp.Id);
                        if (EmpData == null)
                        {
                            continue;
                        }
                        if (emp.Role == "static")
                        {
                            var newMonthlyData = new MonthlyEmployeeData()
                            {
                                EmployeeId = emp.Id,
                                Month = egyptDate.Month,
                                Year = egyptDate.Year,
                                TotalSalary = EmpData.TotalSalary,
                                Holidaies = EmpData.Holidaies,
                                Target = EmpData.Target,
                                
                            };
                            await _dbContext.MonthlyEmployeeData.AddAsync(newMonthlyData);
                        }
                        else if (emp.Role == "changable" || emp.Role == "delivery")
                        {
                            var newMonthlyData = new MonthlyEmployeeData()
                            {
                                EmployeeId = emp.Id,
                                Month = egyptDate.Month,
                                Year = egyptDate.Year,
                                SalaryPerHour = EmpData.SalaryPerHour,
                                Holidaies = EmpData.Holidaies,
                                TotalSalary = 0.0,
                                Target = EmpData.Target,
                                
                            };
                            await _dbContext.MonthlyEmployeeData.AddAsync(newMonthlyData);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
