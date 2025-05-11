using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Context;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IGenericRepository<WorkLogs> _worklogsRepo;
        private readonly IGenericRepository<Employee> _empRepo;
        private readonly IGenericRepository<MonthlyEmployeeData> _monthlyEmpRepo;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HRDbContext _context;

        public HRController(IMapper mapper, IGenericRepository<WorkLogs> worklogsRepo, IGenericRepository<Employee> EmpRepo, IGenericRepository<MonthlyEmployeeData> MonthlyEmpRepo, UserManager<User> userManager, RoleManager<IdentityRole> roleManager,HRDbContext context)
        {
            _mapper = mapper;
            _worklogsRepo = worklogsRepo;
            _empRepo = EmpRepo;
            _monthlyEmpRepo = MonthlyEmpRepo;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }
        public IActionResult Index(string? message = null)
        {
            ViewBag.Message = message;
            return View();
        }

        [HttpGet]
        public IActionResult AddEmployee() 
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddEmployee(EmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userName = model.UserName?.Trim().Replace(" ", "");
            var password = model.Password;

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return RedirectToAction("Index", new { message = "لم يتم اضافه اليوزر بنجاح قد يكون هناك مسافات او قيم فارغه" });
            }

            if (!await _roleManager.RoleExistsAsync("Employee"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Employee"));
                if (!roleResult.Succeeded)
                {
                    return RedirectToAction("Index", new { message = "حدث خطأ في انشاء الدور" });
                }
            }

            var existingUser = await _userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                return RedirectToAction("Index", new { message = "المستخدم موجود بالفعل" });
            }

            var user = new User 
            {
                UserName = userName
            };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { message = "حدث خطأ في انشاء المستخدم" });
            }

            var roleAssignResult = await _userManager.AddToRoleAsync(user, "Employee");
            if (!roleAssignResult.Succeeded)
            {
                return RedirectToAction("Index", new { message = "لم نستطع اضافه المستخدم للدور المنسوب اليه" });
            }

            var employee = new Employee
            {
                Name = model.Name,
                BranchName = model.BranchName,
                Role = model.Role,
                BankName = model.BankName,
                BankAccount = model.BankAccount,
                UserId = user.Id,
               

            };

            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            var addedEmployee = await _context.Employees
                .Where(e => e.Name == model.Name)
                .OrderByDescending(e => e.Id) 
                .FirstOrDefaultAsync();
            if (addedEmployee == null)
            {
                return RedirectToAction("Index", new { message = "تم فقد الموظف عند عمليه الاضافه" });
            }

            var employeeId = addedEmployee.Id;
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            if (model.Role == "static")
            {
                var monthlyData = new MonthlyEmployeeData
                {
                    EmployeeId = employeeId,
                    TotalSalary = model.TotalSalary,
                    Month = currentMonth,
                    Year = currentYear,
                    Holidaies = 7,
                    Target = (model.Target) * 26,
                    NetSalary = 0.0
                };
                await _context.MonthlyEmployeeData.AddAsync(monthlyData);
            }
            else if (model.Role == "changable" || model.Role == "delivery")
            {
                var monthlyData = new MonthlyEmployeeData
                {
                    EmployeeId = employeeId,
                    SalaryPerHour = model.SalaryPerHour,
                    Month = currentMonth,
                    Year = currentYear,
                    Holidaies = 7,
                    TotalSalary = (model.SalaryPerHour * model.Hours??0) / 26,
                    Target = (model.Target) * 26,
                    NetSalary = 0.0
                };
                _context.MonthlyEmployeeData.Add(monthlyData);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { message = "تم اضافه الموظف بنجاح" });
        }
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees(int? month = null, int? year = null, string? BranchName = null,string? message = null , string? BankName = null , string? Name = null, string? Role = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;

            ViewBag.CurrentMonth = currentMonth;
            ViewBag.CurrentYear = currentYear;
            TempData["FilterMonth"] = currentMonth;
            TempData["FilterYear"] = currentYear;
            TempData["FilterName"] = Name;

            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(currentMonth, currentYear, BranchName,BankName,Role,Name));
            var MappedEmployess = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees);

            foreach (var employee in MappedEmployess)
            {
                var totalDiscounts = _context.Discounts.Where(d => d.MonthlyEmployeeDataId == employee.MonthlyEmployeeDataId)
                                                       .Where(s => s.year == currentYear && s.month == currentMonth).Sum(b => b.Amount);
                var totalBounss = _context.Bounss.Where(d => d.MonthlyEmployeeDataId == employee.MonthlyEmployeeDataId)
                                                 .Where(s => s.year == currentYear && s.month == currentMonth).Sum(b => b.Amount);
                var totalBorrows = _context.Borrows.Where(d => d.MonthlyEmployeeDataId == employee.MonthlyEmployeeDataId)
                                                   .Where(s => s.year == currentYear && s.Month == currentMonth).Sum(b => b.Amount);
                employee.TotalDiscounts = totalDiscounts;
                employee.TotalBonuss = totalBounss;
                employee.TotalBorrows = totalBorrows;
                if (employee?.Role.ToLower() == "static")
                {
                    employee.NetSalary = (employee.TotalSalary + totalBounss)
                    - (totalBorrows + (double)totalDiscounts );

                }
                else if (employee?.Role.ToLower() == "changable")
                {
                    employee.NetSalary = ((((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0)+ (employee.ForgetedHours??0)) * employee.SalaryPerHour / 26)
                    + totalBounss) - (totalBorrows + (double)totalDiscounts);
                    employee.TotalSalary = (((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0) + (employee.ForgetedHours ?? 0)) * employee.SalaryPerHour) / 26;
                }
                else if (employee?.Role.ToLower() == "delivery")
                {
                    employee.NetSalary = ((((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0) + (employee.ForgetedHours??0)) * employee.SalaryPerHour)
                    + totalBounss) - (totalBorrows + (double)totalDiscounts);
                    employee.TotalSalary = ((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0) + (employee.ForgetedHours??0))  * employee.SalaryPerHour;

                }
                if (employee.Holidaies == null)
                {
                    employee.Holidaies = 7;
                }
                if (employee.Target == null || employee.Target == 0)
                {
                    employee.Target = 8*26;
                }

                var monthlyData = await _context.MonthlyEmployeeData
                    .FirstOrDefaultAsync(m => m.EmployeeId == employee.Id && m.Month == currentMonth && m.Year == currentYear);
                
                if (monthlyData != null)
                {
                    monthlyData.NetSalary = employee.NetSalary;
                    monthlyData.TotalSalary = employee.TotalSalary;
                    monthlyData.Holidaies = employee.Holidaies;
                    monthlyData.Target = employee.Target;
                    monthlyData.TotalDiscounts = employee.TotalDiscounts;
                    monthlyData.TotalBorrows = employee.TotalBorrows;
                    monthlyData.TotalBouns = employee.TotalBonuss;
                    _context.MonthlyEmployeeData.Update(monthlyData);
                }
            }

            await _context.SaveChangesAsync();
            var employees2 = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(currentMonth, currentYear, BranchName, BankName, Role, Name));
            var MappedEmployess2 = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees2);
            
            var uniqueBranchNames = employees2
                .Where(e => !string.IsNullOrEmpty(e.BranchName))
                .Select(e => e.BranchName)
                .Distinct()
                .ToList();

            var uniqBankName = employees2.Where(e => !string.IsNullOrEmpty(e.BankName))
                .Select(e => e.BankName)
                .Distinct()
                .ToList();
            var uniqRole = employees2.Where(e => !string.IsNullOrEmpty(e.Role))
                .Select(e => e.Role)
                .Distinct()
                .ToList();
            ViewData["BranchNames"] = uniqueBranchNames;
            ViewData["BankNames"] = uniqBankName;
            ViewData["Roles"] = uniqRole;
            ViewBag.Message = message;
            return View(MappedEmployess2);
        }
        [HttpGet]
        public async Task<IActionResult> UpdateEmployee(int id, int? month = null, int? year = null)
        {
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(id, month, year));
            var mappedEmployee = _mapper.Map<EmployeeViewModel>(employee);
            TempData["FilterName"] = employee.Name;
            TempData.Keep("FilterName");
            return View(mappedEmployee);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(EmployeeViewModel model)
        {
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(model.Id));
            if (employee == null)
            {
                return RedirectToAction("GetAllEmployees", new { message = "الموظف غير موجود" });
            }

            employee.Name = model.Name;
            employee.BranchName = model.BranchName;
            employee.Role = model.Role;
            employee.BankName = model.BankName;
            employee.BankAccount = model.BankAccount;

            var monthlyData = employee.MonthlyData
                        .OrderByDescending(md => md.Year)
                        .ThenByDescending(md => md.Month)
                        .FirstOrDefault(md => md.Month == model.Month && md.Year == model.Year);

            if (monthlyData != null)
            {
                monthlyData.Hours = model.Hours;
                monthlyData.HoursOverTime = model.HoursOverTime;
                monthlyData.ForgetedHours = model.ForgetedHours;
                monthlyData.SalaryPerHour = model.SalaryPerHour;
                monthlyData.TotalSalary = model.TotalSalary;
                monthlyData.Holidaies = model.Holidaies;

                var totalDiscounts = employee.MonthlyData
                    .Where(md => md.Id == monthlyData.Id)
                    .SelectMany(md => md.Discounts)
                    .Sum(d => d.Amount);

                var totalBorrows = employee.MonthlyData
                    .Where(md => md.Id == monthlyData.Id)
                    .SelectMany(md => md.Borrows)
                    .Sum(b => b.Amount);

                var totalBounss = employee.MonthlyData
                    .Where(md => md.Id == monthlyData.Id)
                    .SelectMany(md => md.Bounss)
                    .Sum(b => b.Amount);
                monthlyData.TotalDiscounts = totalDiscounts;
                monthlyData.TotalBorrows = totalBorrows;
                monthlyData.TotalBouns = totalBounss;
                monthlyData.Target = model.Target;
            }
            else
            {
                return View(model);
            }

            _empRepo.Update(employee);
            int result = await _empRepo.CompleteAsync();

            if (result > 0)
            {
                return RedirectToAction("GetAllEmployees",new { message = "تم التعديل بنجاح"});
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult GetAllHolidaysRequests(string? message)
        {
            var requests = _context.HolidayRequests.Where(hr => hr.status == "Pending")
            .Include(hr => hr.Employee)
            .ToList();
            ViewBag.Message = message;
            var MappedRequests = _mapper.Map<List<HolidayRequestViewModel>>(requests);

            return View(MappedRequests);
        }
        [HttpGet]
        public IActionResult GetAllHolidaysArchive(string? message)
        {
            var requests = _context.HolidayRequests
                .Where(hr => hr.status == "مرفوض" || hr.status == "مقبول")
                .Include(hr => hr.Employee)
                .ToList();

            var MappedRequests = _mapper.Map<List<HolidayRequestViewModel>>(requests);
            ViewBag.Message = message;
            return View(MappedRequests);
        }
        [HttpGet]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var holidayRequest = await _context.HolidayRequests
                .Include(hr => hr.Employee)
                .FirstOrDefaultAsync(hr => hr.Id == id);


            if (holidayRequest == null)
            {
                return RedirectToAction("GetAllHolidaysRequests" , new { message = "غير موجود من المحتمل حدث خطأ" });
            }

            var MappedRequest = _mapper.Map<HolidayRequestViewModel>(holidayRequest);

            return View(MappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveRequest(HolidayRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(model.EmployeeId));
                if (employee == null)
                {
                    return RedirectToAction("GetAllHolidaysRequests", new { message = "الموظف غير موجود يبدو ان حدث خطأ" });
                }

                var temp = employee.MonthlyData
                                  .OrderByDescending(m => m.Year)
                                  .ThenByDescending(m => m.Month)
                                  .FirstOrDefault(); 
                if (temp != null)
                {
                    if (temp.Holidaies > 0 || temp.Holidaies==null)
                    {
                        if (temp.Holidaies == null)
                        {
                            temp.Holidaies = 7;
                            temp.Holidaies -= 1;
                        }
                        else
                        {
                            temp.Holidaies -= 1;
                        }
                        _empRepo.Update(employee);
                    }
                    else
                    {
                        return RedirectToAction("GetAllHolidaysRequests", new { message = "لا يوجد اجازات متاحه" });
                    }
                }

                var request = await _context.HolidayRequests.FindAsync(model.Id);
                if (request == null)
                {
                    return RedirectToAction("GetAllHolidaysRequests", new { message = "الطلب غير موجود يبدو ان حدث خطأ" });
                }

                request.status = "مقبول";
                _context.HolidayRequests.Update(request);

                int Result = await _empRepo.CompleteAsync();
                if (Result > 0)
                {
                    return RedirectToAction("GetAllHolidaysRequests", new { message = "تم الموافقة على الطلب" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var holidayRequest = await _context.HolidayRequests
                .Include(hr => hr.Employee)
                .FirstOrDefaultAsync(hr => hr.Id == id);


            if (holidayRequest == null)
            {
                return RedirectToAction("GetAllHolidaysRequests", new { message = "غير موجود من المحتمل حدث خطأ" });
            }

            var MappedRequest = _mapper.Map<HolidayRequestViewModel>(holidayRequest);

            return View(MappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> RejectRequest(HolidayRequestViewModel model) 
        {
            if (ModelState.IsValid)
            {
                var request = await _context.HolidayRequests.FindAsync(model.Id);
                if (request == null)
                {
                    return RedirectToAction("GetAllHolidaysRequests", new { message = "الطلب غير موجود يبدو ان حدث خطأ" });
                }

                request.status = "مرفوض";
                request.ReasonOfRejection = model.ReasonOfRejection;
                _context.HolidayRequests.Update(request);
                int Result = await _context.SaveChangesAsync();

                if (Result > 0)
                {
                    return RedirectToAction("GetAllHolidaysRequests", new { message = "تم الرفض على الطلب" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult GetAllDiscounts(int monthlyEmployeeDataId, int month, int year,string name)
        {
            var discounts = _context.Discounts
                                    .Where(d => d.MonthlyEmployeeDataId == monthlyEmployeeDataId)
                                    .ToList();

            var discountViewModels = _mapper.Map<List<DiscountViewModel>>(discounts);
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            return View(discountViewModels);
        }
        [HttpGet]
        public IActionResult GetAllBouns(int monthlyEmployeeDataId,int month , int year,string name)
        {
            var bouns = _context.Bounss
                        .Where(b => b.MonthlyEmployeeDataId == monthlyEmployeeDataId)
                        .ToList();

            var bounsViewModels = _mapper.Map<List<BounsViewModel>>(bouns);
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            return View(bounsViewModels);
        }
        [HttpGet]
        public IActionResult GetAllBorrows(int monthlyEmployeeDataId, int month, int year, string name)
        {
            var borrows = _context.Borrows
                          .Where(b => b.MonthlyEmployeeDataId == monthlyEmployeeDataId)
                          .ToList();

            var borrowsViewModels = _mapper.Map<List<BorrowViewModel>>(borrows);
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            return View(borrowsViewModels);

        }
        [HttpPost]
        public async Task<IActionResult> DeleteDiscount(int discountId,int month,int year,string name)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            var discount = await  _context.Discounts.Where(d => d.Id == discountId).FirstOrDefaultAsync();

            if (discount == null)
            {
                return RedirectToAction("GetAllEmployees", new { month = month, year = year , Name = name});
            }

            int MonthlyDataId = discount.MonthlyEmployeeDataId;

            _context.Discounts.Remove(discount);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , name = name});
            }
            return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , name = name});
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBorrow(int borrowId, int month, int year, string name)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            var borrow = await _context.Borrows.Where(d => d.Id == borrowId).FirstOrDefaultAsync();
            int MonthlyDataId = borrow.MonthlyEmployeeDataId;
            if (borrow == null)
            {
                return RedirectToAction("GetAllBorrows",new { monthlyEmployeeDataId = MonthlyDataId, month = month, year = year , name = name});
            }
            

            _context.Borrows.Remove(borrow);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
               return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , name = name});
            }

            return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year ,name = name});
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBouns(int bonusId, int month, int year, string name)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            var bouns = await _context.Bounss.Where(d => d.Id == bonusId).FirstOrDefaultAsync();
            int MonthlyDataId = bouns.MonthlyEmployeeDataId;
            if (bouns == null)
            {
                return RedirectToAction("GetAllBouns",new { monthlyEmployeeDataId = MonthlyDataId, month = month, year = year , name = name});
            }

            

            _context.Bounss.Remove(bouns);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year, name = name });
            }

            return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = MonthlyDataId, month = month, year = year , name = name});
        }
        [HttpGet]
        public IActionResult AddDiscount(int monthlyEmployeeDataId,int month,int year, string name)
        {
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("MonthlyEmployeeDataId");
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddDiscount(DiscountViewModel model,int month,int year,string name)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year , name = name });
            }
            var discount = new Discounts
            {
                Amount = model.Amount,
                ReasonOfDiscount = model.ReasonOfDiscount,
                Notes = model.Notes??"",
                Date = egyptDateTime,
                MonthlyEmployeeDataId = model.MonthlyEmployeeDataId,
                month = monthlyData.Month,
                year = monthlyData.Year,
            };

            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year , name = name });
        }
        [HttpGet]
        public IActionResult AddBouns(int monthlyEmployeeDataId,int month,int year,string name)
        {
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("MonthlyEmployeeDataId");
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddBouns(BounsViewModel model, int month, int year, string name)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year,name = name });
            }
            
            var bouns = new Bouns
            {
                Amount = model.Amount,
                Notes = model.Notes ?? "",
                MonthlyEmployeeDataId = model.MonthlyEmployeeDataId,
                DateOfBouns = egyptDateTime,
                Reason = model.Reason,
                month = monthlyData.Month,
                year = monthlyData.Year,
            };

            await _context.Bounss.AddAsync(bouns);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year,name = name });
        }
        [HttpGet]
        public IActionResult AddBorrow(int monthlyEmployeeDataId, int month, int year, string name)
        {
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("MonthlyEmployeeDataId");
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddBorrow(BorrowViewModel model, int month, int year, string name)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterName"] = name;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterName");
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            var monthlyData = await _context.MonthlyEmployeeData
                        .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId , month = month , year = year , name = name});
            }
            var borrow = new Borrow
            {
                Amount = model.Amount,
                Notes = model.Notes ?? "",
                MonthlyEmployeeDataId = model.MonthlyEmployeeDataId,
                DateOfBorrow = egyptDateTime,
                Reason = model.Reason,
                Month = monthlyData.Month,
                year = monthlyData.Year,
            };

            await _context.Borrows.AddAsync(borrow);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year , name = name});
        }
        [HttpGet]
        public IActionResult GetAllPendResignations(string? message) 
        {
            var resignations = _context.ResignationRequests
                .Where(r => r.status == "pending")
                .Include(r => r.Employee)
                .ToList();

            var mappedResignations = _mapper.Map<IEnumerable<ResignationRequestsViewModel>>(resignations);
            ViewBag.Message = message;
            return View(mappedResignations);
        }
        [HttpGet]
        public IActionResult GetResignationsArchive() 
        {
            var resignations = _context.ResignationRequests
                .Where(r => r.status == "مرفوض" || r.status == "مقبول")
                .Include(r => r.Employee)
                .ToList();

            var mappedResignations = _mapper.Map<List<ResignationRequestsViewModel>>(resignations);
            return View(mappedResignations);
        }
        [HttpGet]
        public async Task<IActionResult> ApproveResignation(int id)
        {
            var resignationRequest = await _context.ResignationRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resignationRequest == null)
            {
                return RedirectToAction("GetAllPendResignations", new { message = "غير موجود من المحتمل حدث خطأ" });
            }

            var mappedRequest = _mapper.Map<ResignationRequestsViewModel>(resignationRequest);

            return View(mappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveResignation(ResignationRequestsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var resignationRequest = await _context.ResignationRequests.FindAsync(model.Id);
                if (resignationRequest == null)
                {
                    return RedirectToAction("GetAllPendResignations", new { message = "الطلب غير موجود يبدو ان حدث خطأ" });
                }

                resignationRequest.status = "مقبول";
                _context.ResignationRequests.Update(resignationRequest);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllPendResignations", new { message = "تم قبول الاستقاله" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RejectResignation(int id)
        {
            var resignationRequest = await _context.ResignationRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resignationRequest == null)
            {
                return RedirectToAction("GetAllPendResignations", new { message = "غير موجود من المحتمل حدث خطأ" });
            }

            var mappedRequest = _mapper.Map<ResignationRequestsViewModel>(resignationRequest);

            return View(mappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> RejectResignation(ResignationRequestsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var resignationRequest = await _context.ResignationRequests.FindAsync(model.Id);
                if (resignationRequest == null)
                {
                    return RedirectToAction("GetAllPendResignations", new { message = "الطلب غير موجود يبدو ان حدث خطأ" });
                }

                resignationRequest.status = "مرفوض";
                resignationRequest.ReasonOfRejection = model.ReasonOfRejection;
                _context.ResignationRequests.Update(resignationRequest);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllPendResignations", new { message = "تم رفض الاستقاله" });
                }
            }
            return View(model);
        }
        // atenddance report
        [HttpGet]
        public async Task<IActionResult> GetAttendanceReport(DateTime? date)
        {
            DateOnly currentDate;
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            if (date == null)
            {
                currentDate = egyptDate;
            }
            else 
            {
                currentDate = DateOnly.FromDateTime(date.Value);
            }
            var AllAttendee = await _worklogsRepo.GetAllWithSpecAsync(new WorkLogsSpec(currentDate));
            var MappedAttendee = _mapper.Map<IEnumerable<WorkLogsViewModel>>(AllAttendee);
            TempData["Date"] = date;
            TempData.Keep("Date");
            return View(MappedAttendee);
        }
        [HttpGet]
        public async Task<IActionResult> GetForgetedAttendeeThatEndShift(DateTime? date)
        {
            DateOnly currentDate;
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            if (date == null)
            {
                currentDate = egyptDate;
            }
            else
            {
                currentDate = DateOnly.FromDateTime(date.Value);
            }
            var AllAttendee = await _worklogsRepo.GetAllWithSpecAsync(new WorkLogsSpec(currentDate));
            var MappedAttendee = _mapper.Map<IEnumerable<WorkLogsViewModel>>(AllAttendee);
            var Result = MappedAttendee.Where(w => w.End == TimeOnly.MinValue);
            TempData["Date"] = date;
            TempData.Keep("Date");
            return View(Result);
        }

        //Forgeted Shifts
        [HttpGet]
        public async Task<IActionResult> GetAllForgetedShiftsRequests(string? message)
        {
            var requests = await _context.requests.Where(r => r.Status == "Pending").Include(s => s.employee).ToListAsync();
            ViewBag.Message = message;

            var mappedRequests = _mapper.Map<IEnumerable<RequestForForgetCloseShiftViewModel>>(requests);
            return View(mappedRequests);
        }
        [HttpGet]
        public async Task<IActionResult> ApproveForgetShift(int id)
        {
            var request = await _context.requests.FindAsync(id);
            if (request == null)
            {
                return RedirectToAction("GetAllForgetedShiftsRequests", new { message = "الطلب غير موجود" });
            }

            var mappedRequest = _mapper.Map<RequestForForgetCloseShiftViewModel>(request);
            return View(mappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveForgetShift(RequestForForgetCloseShiftViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var request = await _context.requests.FindAsync(model.Id);
                if (request == null)
                {
                    return RedirectToAction("GetAllForgetedShiftsRequests", new { message = "الطلب غير موجود" });
                }

                request.Status = "مقبول";
                _context.requests.Update(request);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllForgetedShiftsRequests", new { message = "تم قبول الطلب" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RejectForgetShift(int id)
        {
            var request = await _context.requests.FindAsync(id);
            if (request == null)
            {
                return RedirectToAction("GetAllForgetedShiftsRequests", new { message = "الطلب غير موجود" });
            }

            var mappedRequest = _mapper.Map<RequestForForgetCloseShiftViewModel>(request);
            return View(mappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> RejectForgetShift(RequestForForgetCloseShiftViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var request = await _context.requests.FindAsync(model.Id);
                if (request == null)
                {
                    return RedirectToAction("GetAllForgetedShiftsRequests", new { message = "الطلب غير موجود" });
                }

                request.Status = "مرفوض";
                request.ReasonOfRejection = model.ReasonOfRejection;
                _context.requests.Update(request);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllForgetedShiftsRequests", new { message = "تم رفض الطلب" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RequestForgtedCloseShiftsArchive() 
        {
            var requests = await _context.requests.Where(r => r.Status == "مقبول" || r.Status == "مرفوض").Include(e => e.employee).ToListAsync();

            var mappedRequests = _mapper.Map<IEnumerable<RequestForForgetCloseShiftViewModel>>(requests);
            return View(mappedRequests);
        }
        //Request for Borrow
        [HttpGet]
        public async Task<IActionResult> GetAllRequestBorrow(string? message)
        {
            var requests = await _context.requestBorrows.Where(r => r.Status == "Pending")
                .Include(r => r.Employee)
                .ToListAsync();

            var mappedRequests = _mapper.Map<IEnumerable<RequestBorrowViewModel>>(requests);
            ViewBag.Message = message;
            return View(mappedRequests);
        }
        [HttpGet]
        public async Task<IActionResult> ApproveRequestBorrow(int id)
        {
            var request = await _context.requestBorrows
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return RedirectToAction("GetAllRequestBorrow", new { message = "الطلب غير موجود" });
            }

            var mappedRequest = _mapper.Map<RequestBorrowViewModel>(request);
            return View(mappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveRequestBorrow(RequestBorrowViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var request = await _context.requestBorrows.FindAsync(model.Id);
                if (request == null)
                {
                    return RedirectToAction("GetAllRequestBorrow", new { message = "الطلب غير موجود" });
                }

                request.Status = "مقبول";
                request.notes = model.notes;
                _context.requestBorrows.Update(request);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllRequestBorrow", new { message = "تم قبول الطلب" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RejectRequestBorrow(int id)
        {
            var request = await _context.requestBorrows
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return RedirectToAction("GetAllRequestBorrow", new { message = "الطلب غير موجود" });
            }

            var mappedRequest = _mapper.Map<RequestBorrowViewModel>(request);
            return View(mappedRequest);
        }
        [HttpPost]
        public async Task<IActionResult> RejectRequestBorrow(RequestBorrowViewModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _context.requestBorrows.FindAsync(model.Id);
                if (request == null)
                {
                    return RedirectToAction("GetAllRequestBorrow", new { message = "الطلب غير موجود" });
                }

                request.Status = "مرفوض";
                request.ReasonOfRejection = model.ReasonOfRejection;
                request.notes = model.notes;
                _context.requestBorrows.Update(request);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllRequestBorrow", new { message = "تم رفض الطلب" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RequestBorrowArchive()
        {
            var requests = await _context.requestBorrows
                .Where(r => r.Status == "مقبول" || r.Status == "مرفوض")
                .Include(r => r.Employee)
                .ToListAsync();

            var mappedRequests = _mapper.Map<IEnumerable<RequestBorrowViewModel>>(requests);
            return View(mappedRequests);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllComplaints(string? message)
        {
            var complaints = await _context.Complaints.Where(c => c.status == "Pending")
                .Include(c => c.Employee)
                .ToListAsync();
            var mappedComplaints = _mapper.Map<IEnumerable<ComplaintsViewModle>>(complaints);
            ViewBag.Message = message;
            return View(mappedComplaints);
        }
        [HttpGet]
        public async Task<IActionResult> GetComplaintsArchive()
        {
            var complaints = await _context.Complaints
                .Where(c => c.status == "مرفوض" || c.status == "مقبول")
                .Include(c => c.Employee)
                .ToListAsync();

            var mappedComplaints = _mapper.Map<IEnumerable<ComplaintsViewModle>>(complaints);
            return View(mappedComplaints);
        }
        [HttpGet]
        public async Task<IActionResult> ApproveComplaint(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return RedirectToAction("GetAllComplaints", new { message = "الطلب غير موجود" });
            }

            var mappedComplaint = _mapper.Map<ComplaintsViewModle>(complaint);
            return View(mappedComplaint);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveComplaint(ComplaintsViewModle model)
        {
            if (!ModelState.IsValid)
            {
                var complaint = await _context.Complaints.FindAsync(model.Id);
                if (complaint == null)
                {
                    return RedirectToAction("GetAllComplaints", new { message = "الطلب غير موجود" });
                }

                complaint.status = "مقبول";
                complaint.response = model.response;
                _context.Complaints.Update(complaint);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllComplaints", new { message = "تم قبول الشكوى" });
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> RejectComplaint(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (complaint == null)
            {
                return RedirectToAction("GetAllComplaints", new { message = "الطلب غير موجود" });
            }

            var mappedComplaint = _mapper.Map<ComplaintsViewModle>(complaint);
            return View(mappedComplaint);
        }
        [HttpPost]
        public async Task<IActionResult> RejectComplaint(ComplaintsViewModle model)
        {
            if (!ModelState.IsValid)
            {
                var complaint = await _context.Complaints.FindAsync(model.Id);
                if (complaint == null)
                {
                    return RedirectToAction("GetAllComplaints", new { message = "الطلب غير موجود" });
                }

                complaint.status = "مرفوض";
                complaint.response = model.response;
                _context.Complaints.Update(complaint);

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllComplaints", new { message = "تم رفض الشكوى" });
                }
            }
            return View(model);
        }
    }
}
