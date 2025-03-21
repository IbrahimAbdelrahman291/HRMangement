using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAllEmployees(string? BranchName = null)
        {
            var CurrentMonth = DateTime.UtcNow.Month;
            var CurrentYear = DateTime.UtcNow.Year;

            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(CurrentMonth, CurrentYear, BranchName));
            var MappedEmployess = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees);
            
            var uniqueBranchNames = employees
                .Where(e => !string.IsNullOrEmpty(e.BranchName))
                .Select(e => e.BranchName)
                .Distinct()
                .ToList();

            var totalNetSalary = MappedEmployess.Sum(e => e.NetSalary); 
            ViewData["TotalNetSalary"] = totalNetSalary;
            ViewData["BranchNames"] = uniqueBranchNames;

            return View(MappedEmployess);
        }
    }
}
