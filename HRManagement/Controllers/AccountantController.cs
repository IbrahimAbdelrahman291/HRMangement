using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "Accountant")]
    public class AccountantController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IGenericRepository<Employee> _empRepo;

        public AccountantController(IMapper mapper, IGenericRepository<Employee> EmpRepo)
        {
            _mapper = mapper;
            _empRepo = EmpRepo;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees(int? month = null,int? year = null, string? BranchName = null, string? Role = null , string? BankName = null , string? Name = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(currentMonth, currentYear, BranchName, BankName, Role, Name));
            var MappedEmployess = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees);

            var uniqueBranchNames = employees
                .Where(e => !string.IsNullOrEmpty(e.BranchName))
                .Select(e => e.BranchName)
                .Distinct()
                .ToList();

            var uniqBankName = employees.Where(e => !string.IsNullOrEmpty(e.BankName))
                .Select(e => e.BankName)
                .Distinct()
                .ToList();
            var uniqRole = employees.Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .ToList();
            ViewData["BranchNames"] = uniqueBranchNames;
            ViewData["BankNames"] = uniqBankName;
            ViewData["Roles"] = uniqRole;

            var totalNetSalary = MappedEmployess.Sum(e => e.NetSalary); 
            ViewData["TotalNetSalary"] = totalNetSalary;

            return View(MappedEmployess);
        }
    }
}
