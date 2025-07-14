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
        private readonly IGenericRepository<WorkLogs> _worklogsRepo;

        public EmployeeController(HRDbContext context, IGenericRepository<Employee> empRepo,IGenericRepository<MonthlyEmployeeData> MonthlyEmpData, IMapper mapper,IGenericRepository<WorkLogs> worklogsRepo)
        {
            _context = context;
            _empRepo = empRepo;
            _monthlyEmpData = MonthlyEmpData;
            _mapper = mapper;
            _worklogsRepo = worklogsRepo;
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

            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);
            var egyptTime = TimeOnly.FromDateTime(egyptDateTime);


            var existingWorkLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.EmployeeId == employeeId && w.Day == egyptDate);
            //مينفعش نمنعه يسجل تاني لو هو سجل امبارح وحاول يسجل تاني انهارده لان ممكن يكون شيفت صباحي ف يبقا كدا الناس بتاعت الشيفت المسائي هما اللي ياخدو بالهم وهما بنهمو الشيفت
            if (existingWorkLog != null)
            {
                TempData.Keep("EmployeeId");
                return RedirectToAction("Index", new { userId = userId, message = "تم تسجيل بداية العمل بالفعل لهذا اليوم." });
            }

            var newWorkLog = new WorkLogs
            {
                EmployeeId = employeeId,
                Day = egyptDate,
                Start = egyptTime
            };

            await _context.WorkLogs.AddAsync(newWorkLog);
            await _context.SaveChangesAsync();

            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");

            return RedirectToAction("Index", new { userId = userId, message = "تم تسجيل بداية العمل بنجاح." });
        }
        [HttpPost]
        public async Task<IActionResult> EndJob()
        {
            if (TempData["EmployeeId"] == null || !int.TryParse(TempData["EmployeeId"].ToString(), out int employeeId))
            {
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob", new { message = "خطأ: لم يتم تحديد الموظف بشكل صحيح." });
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (employee == null)
            {
                TempData.Keep("EmployeeId");
                return NotFound("Employee not found in the database.");
            }

            string userId = employee.UserId;

            try
            {
                var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

                var egyptDate = DateOnly.FromDateTime(egyptDateTime);
                var egyptTime = TimeOnly.FromDateTime(egyptDateTime);
                var currentmonth = egyptDate.Month;
                var currentyear = egyptDate.Year;

                var workLog = await _context.WorkLogs
                    .FirstOrDefaultAsync(w => w.EmployeeId == employeeId
                                              && w.Day == egyptDate
                                              && w.End == TimeOnly.MinValue);

                if (workLog == null)
                {
                    var previousDate = egyptDate.AddDays(-1);
                    workLog = await _context.WorkLogs
                        .FirstOrDefaultAsync(w => w.EmployeeId == employeeId
                                                  && w.Day == previousDate
                                                  && w.End == TimeOnly.MinValue);

                    if (workLog == null)
                    {
                        TempData["EmployeeId"] = employeeId;
                        TempData.Keep("EmployeeId");
                        return RedirectToAction("ManageJob", new { message = "لا يوجد تسجيل حضور في هذا اليوم أو اليوم الذي يسبقه" });
                    }
                }

                workLog.End = egyptTime;

                DateTime startDateTime = workLog.Day.ToDateTime(workLog.Start);
                DateTime endDateTime = egyptDateTime;
                var totalWorkTime = endDateTime - startDateTime;

                if (totalWorkTime.TotalHours >= 24)
                {
                    TempData["EmployeeId"] = employeeId;
                    TempData.Keep("EmployeeId");
                    return RedirectToAction("ManageJob", new { message = "تعذر تسجيل ساعاتك، يجب التواصل مع HR" });
                }

                workLog.TotalTime = totalWorkTime;

                var employeeData = await _context.MonthlyEmployeeData.Where(m => m.Month == currentmonth && m.Year == currentyear)
                                                 .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
                if (employeeData != null)
                {
                    employeeData.Hours = (employeeData.Hours ?? 0) + totalWorkTime.TotalHours;
                    _monthlyEmpData.Update(employeeData);
                }

                int result = await _monthlyEmpData.CompleteAsync();
                if (result > 0)
                {
                    return RedirectToAction("Index", new { userId = userId, message = "تم انهاء الشيفت بنجاح" });
                }
            }
            catch
            {
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                return RedirectToAction("ManageJob", new { message = "حدث خطأ في انهاء الشيفت" });
            }

            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return RedirectToAction("Index", new { userId = userId, message = "تعذر انهاء الشيفت" });
        }
        [HttpGet]
        public async Task<IActionResult> MyData(int? employeeId = null, int? month = null, int? year = null,string? userId = null)
        {

            int? currentMonth = month;
            int? currentYear = year;
            int EmpId = employeeId ?? (int)TempData["EmployeeId"]!;
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(EmpId, currentMonth, currentYear));

            if (employee == null)
            {
                return RedirectToAction("Index", new { userId = userId,message = "لم يتم العثور على الموظف" });
            }
            var UserId = employee.UserId;

            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            TempData.Keep("EmployeeId");
            var mappedEmployee = _mapper.Map<EmployeeViewModel>(employee);
            return View(mappedEmployee);
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

                return RedirectToAction("Index", new { userId = employee.UserId, message = "تم تقديم الطلب بنجاح" });
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
                .Where(rr => rr.EmployeeId == employeeId).Where(s => s.status == "pending" || s.status == "مقبول")
                .ToList();
            if (resignationRequests.Count > 0)
            {
                return RedirectToAction("MyResignationRequest", new { employeeId = employeeId, message = "يوجد استقاله بالفعل مقدمه وقيد الانتظار او تم قبولها" });
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
                return RedirectToAction("Index", new { userId = employee.UserId, message = "تم تقديم الاستقاله بنجاح" });
            }
            TempData.Keep("EmployeeId");
            return View(model);
        }
        [HttpGet]
        public async Task<ActionResult<List<DiscountViewModel>>> GetAllDiscounts(int employeeId, int? month = null, int? year = null, string? message = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId, currentMonth, currentYear));
            if (employee == null)
            {
                return RedirectToAction("GetAllDiscounts", new { employeeId = employeeId, month = egyptDate.Month, year = egyptDate.Year, message = "لا يوجد بيانات لهذا الشهر" });
            }
            var mappedEmployee = _mapper.Map<EmployeeViewModel>(employee);
            var monthlyDataId = mappedEmployee.MonthlyEmployeeDataId;
            var discounts = _context.Discounts
                .Where(d => d.MonthlyEmployeeDataId == monthlyDataId)
                .ToList();
            if (discounts == null)
            {
                return RedirectToAction("GetAllDiscounts", new { employeeId = employeeId, month = egyptDate.Month, year = egyptDate.Year, message = "لا يوجد خصومات لهذا الشهر" });
            }
            var mappedDiscounts = _mapper.Map<List<DiscountViewModel>>(discounts);
            ViewBag.Message = message;
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View(mappedDiscounts);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBonus(int employeeId, int? month = null, int? year = null, string? message = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId, currentMonth, currentYear));
            if (employee == null)
            {
                return RedirectToAction("GetAllBonus", new { employeeId = employeeId, month = egyptDate.Month, year = egyptDate.Year, message = "لا يوجد بيانات لهذا الشهر" });
            }
            var mappedEmployee = _mapper.Map<EmployeeViewModel>(employee);
            var monthlyDataId = mappedEmployee.MonthlyEmployeeDataId;
            var bonus = _context.Bounss
                .Where(d => d.MonthlyEmployeeDataId == monthlyDataId)
                .ToList();
            if (bonus == null)
            {
                return RedirectToAction("GetAllBonus", new { employeeId = employeeId, month = egyptDate.Month, year = egyptDate.Year, message = "لا يوجد زيادات لهذا الشهر" });
            }
            var mappedBonus = _mapper.Map<List<BounsViewModel>>(bonus);
            ViewBag.Message = message;
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View(mappedBonus);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBorrows(int employeeId, int? month = null, int? year = null, string? message = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId, currentMonth, currentYear));
            if (employee == null)
            {
                return RedirectToAction("GetAllBorrows", new { employeeId = employeeId, month = egyptDate.Month, year = egyptDate.Year, message = "لا يوجد بيانات لهذا الشهر" });
            }
            var mappedEmployee = _mapper.Map<EmployeeViewModel>(employee);
            var monthlyDataId = mappedEmployee.MonthlyEmployeeDataId;
            var borrows = _context.Borrows
                .Where(d => d.MonthlyEmployeeDataId == monthlyDataId)
                .ToList();
            if (borrows == null)
            {
                return RedirectToAction("GetAllBorrows", new { employeeId = employeeId, month = egyptDate.Month, year = egyptDate.Year, message = "لا يوجد بيانات لهذا الشهر" });
            }
            var mappedBorrows = _mapper.Map<List<BorrowViewModel>>(borrows);
            ViewBag.Message = message;
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View(mappedBorrows);
        }
        [HttpGet]
        public IActionResult GetAllRequestsForFogetCloseShift(int employeeId, string? message)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            ViewBag.Message = message;
            var employee = _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.Result.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            var requests = _context.requests.Where(r => r.EmployeeId == employeeId).ToList();

            var MappedRequests = _mapper.Map<List<RequestForForgetCloseShiftViewModel>>(requests);
            return View(MappedRequests);
        }
        [HttpGet]
        public IActionResult RequestForForgetCloseShift(int employeeId)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RequestForForgetCloseShift(RequestForForgetCloseShiftViewModel model)
        {
            if (!ModelState.IsValid)
            {
                int employeeId = (int)TempData["EmployeeId"]!;
                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
                var existingRequest = _context.requests
                    .FirstOrDefault(r => r.EmployeeId == employeeId && r.RequestDate.Date == model.RequestDate.Date);
                if (existingRequest != null)
                {
                    return RedirectToAction("GetAllRequestsForFogetCloseShift", new { employeeId = employeeId, message = "يوجد طلب مقدم بنفس التاريخ" });
                }
                var request = new RequestForForgetCloseShift()
                {
                    EmployeeId = employeeId,
                    RequestDate = model.RequestDate,
                    Reason = model.Reason,
                    Status = "Pending",
                };
                await _context.requests.AddAsync(request);
                await _context.SaveChangesAsync();
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                return RedirectToAction("GetAllRequestsForFogetCloseShift", new { employeeId = employeeId, message = "تم تقديم الطلب بنجاح" });
            }
            TempData.Keep("EmployeeId");
            return View(model);
        }
        [HttpGet]
        public IActionResult GetAllRequestBorrows(int employeeId, string? message = null)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            ViewBag.Message = message;
            var employee = _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.Result.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            var requests = _context.requestBorrows.Where(r => r.EmployeeId == employeeId).ToList();
            var MappedRequests = _mapper.Map<List<RequestBorrowViewModel>>(requests);
            return View(MappedRequests);
        }
        [HttpGet]
        public IActionResult RequestBorrow(int employeeId)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RequestBorrow(RequestBorrowViewModel model)
        {
            if (!ModelState.IsValid)
            {
                int employeeId = (int)TempData["EmployeeId"]!;
                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));

                var request = new RequestBorrow()
                {
                    EmployeeId = employeeId,
                    RequestDate = model.RequestDate,
                    Reason = model.Reason,
                    Status = "Pending",
                    Amount = model.Amount,

                };
                await _context.requestBorrows.AddAsync(request);
                await _context.SaveChangesAsync();
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                return RedirectToAction("GetAllRequestBorrows", new { employeeId = employeeId, message = "تم تقديم الطلب بنجاح" });
            }
            TempData.Keep("EmployeeId");
            return View(model);
        }
        [HttpGet]
        public IActionResult GetAllComplaints(int employeeId, string? message = null)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            ViewBag.Message = message;
            var employee = _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.Result.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            var complaints = _context.Complaints
                .Where(c => c.EmployeeId == employeeId)
                .ToList();
            var mappedComplaints = _mapper.Map<List<ComplaintsViewModle>>(complaints);
            return View(mappedComplaints);
        }
        [HttpGet]
        public IActionResult AddComplaint(int employeeId)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddComplaint(ComplaintsViewModle modle)
        {
            if (!ModelState.IsValid)
            {
                int employeeId = (int)TempData["EmployeeId"]!;
                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
                var Complaints = new Complaints()
                {
                    EmployeeId = employeeId,
                    Date = DateTime.UtcNow,
                    content = modle.content,
                    status = "Pending",
                };
                await _context.Complaints.AddAsync(Complaints);
                int result = await _context.SaveChangesAsync();
                TempData["EmployeeId"] = employeeId;
                TempData.Keep("EmployeeId");
                if (result > 0)
                {
                    return RedirectToAction("GetAllComplaints", new { employeeId = employeeId, message = "تم تقديم الطلب بنجاح" });
                }
                else
                {
                    TempData["EmployeeId"] = employeeId;
                    TempData.Keep("EmployeeId");
                    return RedirectToAction("GetAllComplaints", new { employeeId = employeeId, message = "فشل تقديم الطلب" });
                }
            }
            return View(modle);
        }

        public async Task<IActionResult> ViewInstructions(int employeeId)
        {
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId));
            var UserId = employee.UserId;
            TempData["UserId"] = UserId;
            TempData.Keep("UserId");
            return View();
        }

        //attendance report
        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(int employeeId,DateTime? StartDate = null, DateTime? EndDate = null, string? EmployeeName = null, string? BranchName = null)
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


            var AllAttendee = await _worklogsRepo.GetAllWithSpecAsync(new WorkLogsSpec(StartDate, EndDate, employeeId, EmployeeName, BranchName));
            AllAttendee = AllAttendee.OrderBy(e => e.Day);
            AllAttendee = AllAttendee.OrderBy(e => e.Start);
            var MappedAttendee = _mapper.Map<IEnumerable<WorkLogsViewModel>>(AllAttendee);
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(employeeId,egyptDate.Month,egyptDate.Year));
            if (employee == null)
            {
                return RedirectToAction("Index", new { userId = employee.UserId, message = "لا يوجد بيانات لهذا الشهر" });
            }
            var userId = employee.UserId;
            TempData["UserId"] = userId;
            TempData.Keep("UserId");
            TempData["EmployeeId"] = employeeId;
            TempData.Keep("EmployeeId");
            return View(MappedAttendee);
        }

    }
}