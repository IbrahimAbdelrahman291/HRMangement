using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Context;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "Control")]
    public class ControlController : Controller
    {
        private readonly HRDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IGenericRepository<WorkLogs> _worklogsRepo;
        private readonly IGenericRepository<Employee> _empRepo;
        public ControlController(HRDbContext dbContext, IMapper mapper, IGenericRepository<WorkLogs> worklogsRepo, IGenericRepository<Employee> EmpRepo)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _worklogsRepo = worklogsRepo;
            _empRepo = EmpRepo;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(DateTime? StartDate = null, DateTime? EndDate = null, int? EmployeeId = null, string? EmployeeName = null, string? BranchName = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            var egyptDate = DateOnly.FromDateTime(egyptDateTime);
            if (StartDate is null || EndDate is null)
            {
                StartDate = egyptDateTime;
                EndDate = egyptDateTime;
            }
            else
            {
                if (StartDate > EndDate)
                {
                    var container = StartDate;
                    StartDate = EndDate;
                    EndDate = container;
                }
                if (StartDate > egyptDateTime || EndDate > egyptDateTime)
                {
                    StartDate = EndDate = egyptDateTime;
                }
            }


            var AllAttendee = await _worklogsRepo.GetAllWithSpecAsync(new WorkLogsSpec(StartDate, EndDate, EmployeeId, EmployeeName, BranchName));
            AllAttendee = AllAttendee.OrderBy(e => e.Day);
            AllAttendee = AllAttendee.OrderBy(e => e.Start);
            var MappedAttendee = _mapper.Map<IEnumerable<WorkLogsViewModel>>(AllAttendee);
            var uniqueBranchNames = _dbContext.WorkLogs
                .Where(e => !string.IsNullOrEmpty(e.Employee.BranchName)).Select(s => s.Employee.BranchName)
                .Distinct()
                .ToList();
            TempData["FilterStartDate"] = StartDate;
            TempData["FilterEndDate"] = EndDate;
            TempData["EmployeeId"] = EmployeeId;
            TempData["EmployeeName"] = EmployeeName;
            TempData["BranchName"] = BranchName;
            TempData["Branches"] = uniqueBranchNames;
            TempData.Keep("FilterStartDate");
            TempData.Keep("FilterEndDate");
            TempData.Keep("EmployeeId");
            TempData.Keep("EmployeeName");
            TempData.Keep("BranchName");

            return View(MappedAttendee);
        }
        [HttpGet]
        public async Task<IActionResult> GetForgetedAttendeeThatEndShift(DateTime? StartDate = null, DateTime? EndDate = null, int? EmployeeId = null, string? EmployeeName = null, string? BranchName = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            var egyptDate = DateOnly.FromDateTime(egyptDateTime);
            if (StartDate is null || EndDate is null)
            {
                StartDate = egyptDateTime;
                EndDate = egyptDateTime;
            }
            else
            {
                if (StartDate > EndDate)
                {
                    var container = StartDate;
                    StartDate = EndDate;
                    EndDate = StartDate;
                }
                if (StartDate > egyptDateTime || EndDate > egyptDateTime)
                {
                    StartDate = EndDate = egyptDateTime;
                }
            }


            var AllAttendee = await _worklogsRepo.GetAllWithSpecAsync(new WorkLogsSpec(StartDate, EndDate, EmployeeId, EmployeeName, BranchName));
            var MappedAttendee = _mapper.Map<IEnumerable<WorkLogsViewModel>>(AllAttendee);
            var Result = MappedAttendee.Where(w => w.End == TimeOnly.MinValue);
            TempData["FilterStartDate"] = StartDate;
            TempData["FilterEndDate"] = EndDate;
            TempData["EmployeeId"] = EmployeeId;
            TempData["EmployeeName"] = EmployeeName;
            TempData["BranchName"] = BranchName;
            TempData.Keep("FilterStartDate");
            TempData.Keep("FilterEndDate");
            TempData.Keep("EmployeeId");
            TempData.Keep("EmployeeName");
            TempData.Keep("BranchName");
            return View(Result);
        }
        [HttpGet]
        public async Task<IActionResult> EmployeeThatDoesntStartShift(DateTime? StartDate = null, DateTime? EndDate = null, int? EmployeeId = null, string? EmployeeName = null, string? BranchName = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            var egyptDate = DateOnly.FromDateTime(egyptDateTime);
            if (StartDate is not null && EndDate is not null)
            {
                egyptDate = DateOnly.FromDateTime((DateTime)StartDate);
            }
            var worklogs = await _worklogsRepo.GetAllWithSpecAsync(new WorkLogsSpec(egyptDate));
            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec());
            List<Employee> employeesWhoDidNotWorkToday;
            if (worklogs.Any() && employees.Any())
            {
                employeesWhoDidNotWorkToday = employees.Where(e => !worklogs.Any(w => w.EmployeeId == e.Id)).ToList();
            }
            else
            {
                employeesWhoDidNotWorkToday = new List<Employee>();
            }
            TempData["FilterStartDate"] = StartDate;
            TempData["FilterEndDate"] = EndDate;
            TempData["EmployeeId"] = EmployeeId;
            TempData["EmployeeName"] = EmployeeName;
            TempData["BranchName"] = BranchName;
            TempData.Keep("FilterStartDate");
            TempData.Keep("FilterEndDate");
            TempData.Keep("EmployeeId");
            TempData.Keep("EmployeeName");
            TempData.Keep("BranchName");
            return View(employeesWhoDidNotWorkToday);

        }

    }
}
