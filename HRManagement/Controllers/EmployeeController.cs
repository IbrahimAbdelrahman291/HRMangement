using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Context;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly HRDbContext _context;
        private readonly IGenericRepository<Employee> _empRepo;
        private readonly IGenericRepository<MonthlyEmployeeData> _monthlyEmpData;
        private readonly IMapper _mapper;

        public EmployeeController(HRDbContext context,IGenericRepository<Employee> empRepo, IGenericRepository<MonthlyEmployeeData> MonthlyEmpData, IMapper mapper)
        {
            _context = context;
            _empRepo = empRepo;
            _monthlyEmpData = MonthlyEmpData;
            _mapper = mapper;
        }
        public async Task<IActionResult> Index(string userId, string? message)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required.");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee != null)
            {
                TempData["EmployeeId"] = employee.Id;
                TempData.Keep("EmployeeId");
            }

            ViewBag.Message = message;

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> ManageJob(int employeeId, string? message)
        {

            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            ViewBag.Message = message;
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> StartJob()
        {
            if (TempData["EmployeeId"] == null || !int.TryParse(TempData["EmployeeId"]!.ToString(), out int employeeId))
            {
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob", new { message = "خطأ: لم يتم تحديد الموظف بشكل صحيح." });
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
            {
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob", new { message = "خطأ: لا يمكن الوصول الى الموظف " });
            }

            string userId = employee.UserId;

            var serverDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var serverTime = TimeOnly.FromDateTime(DateTime.UtcNow);

            var existingWorkLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId && w.Day == serverDate);

            if (existingWorkLog != null)
            {
                TempData.Keep("EmployeeId");
                return RedirectToAction("Index", new { userId = userId, message = "تم تسجيل بداية العمل بالفعل لهذا اليوم." });
            }

            var newWorkLog = new WorkLogs
            {
                EmployeeId = employeeId,
                Day = serverDate,
                Start = serverTime
            };

            await _context.WorkLogs.AddAsync(newWorkLog);
            await _context.SaveChangesAsync();

            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");

            return RedirectToAction("Index", new { userId = userId, message = "تم تسجيل بداية العمل بنجاح." });
        }

        public async Task<IActionResult> EndJob()
        {
            if (TempData["EmployeeId"] == null || !int.TryParse(TempData["EmployeeId"].ToString(), out int employeeId))
            {
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
            {
                TempData.Keep("EmployeeId");
                return NotFound("Employee not found in the database.");
            }

            string userId = employee.UserId; 

            var serverDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var serverTime = TimeOnly.FromDateTime(DateTime.UtcNow);

            var workLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId
                                          && w.Day == serverDate
                                          && w.End == TimeOnly.MinValue);

            if (workLog == null)
            {
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob");
            }

            try
            {
                workLog.End = serverTime;
                var totalWorkTime = serverTime.ToTimeSpan() - workLog.Start.ToTimeSpan();
                workLog.TotalTime = totalWorkTime;

                var employeeData = await _context.MonthlyEmployeeData
                                                 .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
                if (employeeData != null)
                {
                    employeeData.Hours += totalWorkTime.TotalHours;
                    if (employeeData.Hours == null)
                    {
                        employeeData.Hours = totalWorkTime.TotalHours;
                    }
                    else
                    {
                        employeeData.Hours += totalWorkTime.TotalHours;
                    }
                    _monthlyEmpData.Update(employeeData);
                }
                int Result = await _monthlyEmpData.CompleteAsync();
                if (Result > 0)
                {
                    return RedirectToAction("Index", new { userId = userId, message = "تم انهاء الشيفت بنجاح"});
                }
            }
            catch
            {
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob",new { message = "حدث خطأ في انهاء الشيفت" });
            }

            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");

            return RedirectToAction("Index", new { userId = userId });
        }
        [HttpGet]
        public async Task<IActionResult> MyData()
        {
            int EmpId;

            if (int.TryParse(TempData["EmployeeId"]?.ToString(),out EmpId))
            {

                TempData.Keep("EmployeeId");
                var month = DateTime.UtcNow.Month;
                var year = DateTime.UtcNow.Year;

                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(EmpId, month, year));
               
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "لا توجد بيانات للموظف.";
                    return RedirectToAction("Index");
                }
                var UserId = employee.UserId;
                TempData["UserId"] = UserId;
                TempData.Keep("UserId");
                var mappedEmployee = _mapper.Map<EmployeeViewModel>(employee);
                return View(mappedEmployee);
            }

            TempData["ErrorMessage"] = "لم يتم العثور على رقم الموظف.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> MyHolidaysRequests(int employeeId, string? message)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            ViewBag.Message = message;
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");

            var holidayRequests = _context.HolidayRequests
                .Where(hr => hr.EmployeeId == employeeId)
                .ToList();
            var mappedHolidayRequests = _mapper.Map<List<HolidayRequestViewModel>>(holidayRequests);

            return View(mappedHolidayRequests);
        }
        [HttpGet]
        public IActionResult RequestHoliday(int employeeId) 
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RequestHoliday(HolidayRequestViewModel model) 
        {
            if (!ModelState.IsValid)
            {
                int employeeId = (int)TempData["EmployeeId"]!;
                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
                var existingRequest = _context.HolidayRequests
                    .FirstOrDefault(hr => hr.EmployeeId == employeeId && hr.HolidayDate.Date == model.HolidayDate.Date);

                if (existingRequest != null)
                {
                    TempData["EmployeeId"] = employeeId;
                    return RedirectToAction("Index", new { userId = employee.UserId, message = "تم تقديم طلب إجازة لهذا التاريخ بالفعل" });
                }

                var holidayRequest = new HolidayRequests
                {
                    EmployeeId = employeeId,
                    HolidayDate = model.HolidayDate,
                    status = "Pending",
                    ReasonOfHoliday = model.ReasonOfHoliday
                };

                await _context.HolidayRequests.AddAsync(holidayRequest);
                await _context.SaveChangesAsync();

                TempData["EmployeeId"] = employeeId; 
                TempData.Keep("EmployeeId");

                return RedirectToAction("Index", new { userId = employee.UserId, message = "تم تقديم الطلب بنجاح"});
            }

            TempData.Keep("EmployeeId");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MyResignationRequest(int employeeId, string? message)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            ViewBag.Message = message;
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            var resignationRequests = _context.ResignationRequests
                .Where(rr => rr.EmployeeId == employeeId)
                .ToList();
            var mappedResignationRequests = _mapper.Map<IEnumerable<ResignationRequestsViewModel>>(resignationRequests);

            return View(mappedResignationRequests);
        }

        [HttpGet]
        public IActionResult AddResignationRequest(int employeeId) 
        {
            var resignationRequests = _context.ResignationRequests
                .Where(rr => rr.EmployeeId == employeeId).Where(s => s.status == "pending" || s.status =="مقبول")
                .ToList();
            if (resignationRequests.Count>0)
            {
                return RedirectToAction("MyResignationRequest", new { employeeId = employeeId , message = "يوجد استقاله بالفعل مقدمه وقيد الانتظار او تم قبولها" });
            }
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddResignationRequest(ResignationRequestsViewModel model) 
        {
            if (!ModelState.IsValid)
            {
                int employeeId = (int)TempData["EmployeeId"]!;
                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));

                #region get current time in Egypt
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime egyptTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, egyptTimeZone);
                #endregion

                var Resignation = new ResignationRequests() 
                {
                    EmployeeId = employeeId,
                    ReasonOfResignation = model.ReasonOfResignation,
                    ResignationDate = egyptTime,
                    status = "pending",
                };
                await _context.ResignationRequests.AddAsync(Resignation);
                await _context.SaveChangesAsync();
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                return RedirectToAction("Index" , new { userId = employee.UserId , message = "تم تقديم الاستقاله بنجاح" });
            }
            TempData.Keep("EmployeeId");
            return View(model);
        }
    }
}