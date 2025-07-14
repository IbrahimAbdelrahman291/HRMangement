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
        private readonly HRDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IGenericRepository<WorkLogs> _worklogsRepo;
        private readonly IGenericRepository<Employee> _empRepo;
        private readonly IGenericRepository<MonthlyEmployeeData> _monthlyEmpRepo;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HRDbContext _context;

        public HRController(HRDbContext dbContext,IMapper mapper, IGenericRepository<WorkLogs> worklogsRepo, IGenericRepository<Employee> EmpRepo, IGenericRepository<MonthlyEmployeeData> MonthlyEmpRepo, UserManager<User> userManager, RoleManager<IdentityRole> roleManager,HRDbContext context)
        {
            _dbContext = dbContext;
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
            TempData["FilterBranch"] = BranchName;

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
                    - (totalBorrows + (double)totalDiscounts + employee.Insurence ?? 0);

                }
                else if (employee?.Role.ToLower() == "changable")
                {
                    employee.NetSalary = ((((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0)+ (employee.ForgetedHours??0)) * employee.SalaryPerHour / 26)
                    + totalBounss) - (totalBorrows + (double)totalDiscounts + employee.Insurence ?? 0);
                    employee.TotalSalary = (((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0) + (employee.ForgetedHours ?? 0)) * employee.SalaryPerHour) / 26;
                }
                else if (employee?.Role.ToLower() == "delivery")
                {
                    employee.NetSalary = ((((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0) + (employee.ForgetedHours??0)) * employee.SalaryPerHour)
                    + totalBounss) - (totalBorrows + (double)totalDiscounts + employee.Insurence ?? 0);
                    employee.TotalSalary = ((employee.Hours ?? 0) + (employee.HoursOverTime ?? 0) + (employee.ForgetedHours??0))  * employee.SalaryPerHour;

                }
                if (employee?.Holidaies == null)
                {
                    employee!.Holidaies = 7;
                }
                if (employee?.Target == null || employee.Target == 0)
                {
                    employee!.Target = 8*26;
                }
                if (employee?.Insurence == null)
                {
                    employee!.Insurence = 255;
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
                    monthlyData.Insurence = employee.Insurence;
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
            TempData["FilterBranch"] = employee.BranchName;
            TempData.Keep("FilterBranch");
            return View(mappedEmployee);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(EmployeeViewModel model,string branch)
        {
            var employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(model.Id,model.Month,model.Year));
            if (employee == null)
            {
                return RedirectToAction("GetAllEmployees", new { message = "الموظف غير موجود" , BranchName = branch });
            }

            employee.Name = model.Name;
            employee.BranchName = model.BranchName;
            employee.Role = model.Role;
            employee.BankName = model.BankName;
            employee.BankAccount = model.BankAccount;

            var monthlyData = employee.MonthlyData.Where(md => md.Month == model.Month && md.Year == model.Year).FirstOrDefault();

            if (monthlyData != null)
            {
                monthlyData.Hours = model.Hours;
                monthlyData.HoursOverTime = model.HoursOverTime;
                monthlyData.ForgetedHours = model.ForgetedHours;
                monthlyData.SalaryPerHour = model.SalaryPerHour;
                monthlyData.TotalSalary = model.TotalSalary;
                monthlyData.Holidaies = model.Holidaies;
                monthlyData.Insurence = model.Insurence;
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
                return RedirectToAction("GetAllEmployees",new { message = "تم التعديل بنجاح", BranchName = employee.BranchName , month = model.Month , year = model.Year});
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
        public IActionResult ApproveRequest(int Id)
        {
            var holidayRequest = _context.HolidayRequests.Where(h => h.Id == Id)
                .Include(hr => hr.Employee).FirstOrDefault();


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
                var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

                var egyptDate = DateOnly.FromDateTime(egyptDateTime);
                var month = egyptDate.Month;
                var year = egyptDate.Year;

                var temp = employee.MonthlyData.Where(m => m.Month == month && m.Year == year).FirstOrDefault();
                if (temp != null)
                {
                    if (temp.Holidaies > 0 || temp.Holidaies==null)
                    {
                        if (temp.Holidaies == null)
                        {
                            temp.Holidaies = 7;
                            temp.Holidaies -= 1;
                            temp.HolidayHours = temp.HolidayHours == null ? 0 + (temp.Target/26) : temp.HolidayHours + (temp.Target/26);
                        }
                        else
                        {
                            temp.Holidaies -= 1;
                            temp.HolidayHours = temp.HolidayHours == null ? 0 + (temp.Target / 26) : temp.HolidayHours + (temp.Target / 26);
                        }
                        _empRepo.Update(employee);
                        _monthlyEmpRepo.Update(temp);
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
                request.HolidayDate = model.HolidayDate;
                _context.HolidayRequests.Update(request);

                int Result = await _dbContext.SaveChangesAsync();
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
        public IActionResult GetAllDiscounts(int monthlyEmployeeDataId, int month, int year,string branch)
        {
            var discounts = _context.Discounts
                                    .Where(d => d.MonthlyEmployeeDataId == monthlyEmployeeDataId)
                                    .ToList();

            var discountViewModels = _mapper.Map<List<DiscountViewModel>>(discounts);
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            return View(discountViewModels);
        }
        [HttpGet]
        public IActionResult GetAllBouns(int monthlyEmployeeDataId,int month , int year,string branch)
        {
            var bouns = _context.Bounss
                        .Where(b => b.MonthlyEmployeeDataId == monthlyEmployeeDataId)
                        .ToList();

            var bounsViewModels = _mapper.Map<List<BounsViewModel>>(bouns);
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            return View(bounsViewModels);
        }
        [HttpGet]
        public IActionResult GetAllBorrows(int monthlyEmployeeDataId, int month, int year, string branch)
        {
            var borrows = _context.Borrows
                          .Where(b => b.MonthlyEmployeeDataId == monthlyEmployeeDataId)
                          .ToList();

            var borrowsViewModels = _mapper.Map<List<BorrowViewModel>>(borrows);
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            return View(borrowsViewModels);

        }
        [HttpPost]
        public async Task<IActionResult> DeleteDiscount(int discountId,int month,int year,string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            var discount = await  _context.Discounts.Where(d => d.Id == discountId).FirstOrDefaultAsync();

            if (discount == null)
            {
                return RedirectToAction("GetAllEmployees", new { month = month, year = year , BranchName = branch });
            }

            int MonthlyDataId = discount.MonthlyEmployeeDataId;

            _context.Discounts.Remove(discount);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , branch = branch });
            }
            return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , branch = branch });
        }
        [HttpGet]
        public async Task<IActionResult> UpdateDiscount(int discountId, int monthlyEmployeeDataId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            TempData.Keep("MonthlyEmployeeDataId");
            var discount = await _context.Discounts.Where(d => d.Id == discountId).FirstOrDefaultAsync();
            if (discount == null)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = monthlyEmployeeDataId, month = month, year = year, branch = branch });
            }
            var mappedDiscount = _mapper.Map<DiscountViewModel>(discount);
            return View(mappedDiscount);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateDiscount(DiscountViewModel model,int id, int monthlyEmployeeDataId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            TempData.Keep("MonthlyEmployeeDataId");
            
            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            discount.Amount = model.Amount;
            discount.ReasonOfDiscount = model.ReasonOfDiscount;
            discount.Notes = model.Notes ?? "";
            discount.Date = model.Date;

            _context.Discounts.Update(discount);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBorrow(int borrowId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            var borrow = await _context.Borrows.Where(d => d.Id == borrowId).FirstOrDefaultAsync();
            int MonthlyDataId = borrow.MonthlyEmployeeDataId;
            if (borrow == null)
            {
                return RedirectToAction("GetAllBorrows",new { monthlyEmployeeDataId = MonthlyDataId, month = month, year = year , branch = branch });
            }
            

            _context.Borrows.Remove(borrow);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
               return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , branch = branch });
            }

            return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year , branch = branch });
        }
        [HttpGet]
        public async Task<IActionResult> UpdateBorrow(int borrowId, int monthlyEmployeeDataId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            TempData.Keep("MonthlyEmployeeDataId");
            var borrow = await _context.Borrows.Where(d => d.Id == borrowId).FirstOrDefaultAsync();
            if (borrow == null)
            {
                return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = monthlyEmployeeDataId, month = month, year = year, branch = branch });
            }
            var mappedBorrow = _mapper.Map<BorrowViewModel>(borrow);
            return View(mappedBorrow);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateBorrow(BorrowViewModel model, int id, int monthlyEmployeeDataId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            TempData.Keep("MonthlyEmployeeDataId");

            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            var borrow = await _context.Borrows.FindAsync(id);
            if (borrow == null)
            {
                return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            borrow.Amount = model.Amount;
            borrow.Reason = model.Reason;
            borrow.Notes = model.Notes ?? "";
            borrow.DateOfBorrow = model.DateOfBorrow;

            _context.Borrows.Update(borrow);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBouns(int bonusId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            var bouns = await _context.Bounss.Where(d => d.Id == bonusId).FirstOrDefaultAsync();
            int MonthlyDataId = bouns.MonthlyEmployeeDataId;
            if (bouns == null)
            {
                return RedirectToAction("GetAllBouns",new { monthlyEmployeeDataId = MonthlyDataId, month = month, year = year , branch = branch });
            }

            

            _context.Bounss.Remove(bouns);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = MonthlyDataId , month = month , year = year, branch = branch });
            }

            return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = MonthlyDataId, month = month, year = year , branch = branch});
        }
        [HttpGet]
        public async Task<IActionResult> UpdateBouns(int bonusId, int monthlyEmployeeDataId, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            TempData.Keep("MonthlyEmployeeDataId");
            var bouns = await _context.Bounss.Where(d => d.Id == bonusId).FirstOrDefaultAsync();
            if (bouns == null)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = monthlyEmployeeDataId, month = month, year = year, branch = branch });
            }
            var mappedBouns = _mapper.Map<BounsViewModel>(bouns);
            return View(mappedBouns);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateBouns(BounsViewModel model, int id, int monthlyEmployeeDataId, int month, int year, string branch) 
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            TempData.Keep("MonthlyEmployeeDataId");

            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            var bouns = await _context.Bounss.FindAsync(id);
            if (bouns == null)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            bouns.Amount = model.Amount;
            bouns.Reason = model.Reason;
            bouns.Notes = model.Notes ?? "";
            bouns.DateOfBouns = model.DateOfBouns;

            _context.Bounss.Update(bouns);
            int result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult AddDiscount(int monthlyEmployeeDataId,int month,int year, string branch)
        {
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("MonthlyEmployeeDataId");
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddDiscount(DiscountViewModel model,int month,int year,string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year , branch = branch });
            }
            var discount = new Discounts
            {
                Amount = model.Amount,
                ReasonOfDiscount = model.ReasonOfDiscount,
                Notes = model.Notes??"",
                Date = model.Date,
                MonthlyEmployeeDataId = model.MonthlyEmployeeDataId,
                month = monthlyData.Month,
                year = monthlyData.Year,
            };

            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetAllDiscounts", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year , branch = branch });
        }
        [HttpGet]
        public IActionResult AddBouns(int monthlyEmployeeDataId,int month,int year,string branch)
        {
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("MonthlyEmployeeDataId");
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddBouns(BounsViewModel model, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var monthlyData = await _context.MonthlyEmployeeData
                .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year,branch = branch });
            }
            
            var bouns = new Bouns
            {
                Amount = model.Amount,
                Notes = model.Notes ?? "",
                MonthlyEmployeeDataId = model.MonthlyEmployeeDataId,
                DateOfBouns = model.DateOfBouns,
                Reason = model.Reason,
                month = monthlyData.Month,
                year = monthlyData.Year,
            };

            await _context.Bounss.AddAsync(bouns);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetAllBouns", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year, branch = branch });
        }
        [HttpGet]
        public IActionResult AddBorrow(int monthlyEmployeeDataId, int month, int year, string branch)
        {
            TempData["MonthlyEmployeeDataId"] = monthlyEmployeeDataId;
            TempData.Keep("MonthlyEmployeeDataId");
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddBorrow(BorrowViewModel model, int month, int year, string branch)
        {
            TempData["FilterMonth"] = month;
            TempData["FilterYear"] = year;
            TempData["FilterBranch"] = branch;
            TempData.Keep("FilterMonth");
            TempData.Keep("FilterYear");
            TempData.Keep("FilterBranch");
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            var monthlyData = await _context.MonthlyEmployeeData
                        .FirstOrDefaultAsync(m => m.Id == model.MonthlyEmployeeDataId);
            if (monthlyData == null)
            {
                return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId , month = month , year = year , branch = branch });
            }
            var borrow = new Borrow
            {
                Amount = model.Amount,
                Notes = model.Notes ?? "",
                MonthlyEmployeeDataId = model.MonthlyEmployeeDataId,
                DateOfBorrow = model.DateOfBorrow,
                Reason = model.Reason,
                Month = monthlyData.Month,
                year = monthlyData.Year,
            };

            await _context.Borrows.AddAsync(borrow);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetAllBorrows", new { monthlyEmployeeDataId = model.MonthlyEmployeeDataId, month = month, year = year , branch = branch });
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
        public async Task<IActionResult> GetAttendanceReport(DateTime? StartDate = null , DateTime? EndDate = null , int? EmployeeId = null,string? EmployeeName = null,string? BranchName = null)
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
        
        //CV
        [HttpGet]
        public async Task<IActionResult> GetAllEmployeeHistories(string? searchName,int? searchId , string? message = null)
        {
            var employees = new List<Employee>();
            if (searchId == null && searchName != null)
            {
                 employees = await _context.Employees.Where(e => e.Name.Contains(searchName)).ToListAsync();
            }
            if (searchId != null && searchName == null)
            {
                employees = await _context.Employees.Where(e => e.Id == searchId).ToListAsync();
            }
            if (searchId != null && searchName != null)
            {
                employees = await _context.Employees.Where(e => e.Id == searchId && e.Name.Contains(searchName)).ToListAsync();
            }
            if (searchId == null && searchName == null)
            {
                employees = await _context.Employees.ToListAsync();
            }
            
            if (employees == null)
            {
                employees = new List<Employee>();
            }
            ViewBag.Message = message;

            return View(employees);
        }
        
        [HttpGet]
        public async Task<IActionResult> CreateEmployeeHistory(int id)
        {
            var existingHistory = await _dbContext.Histories
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (existingHistory != null)
            {
                return RedirectToAction("GetAllEmployeeHistories",new { message = "تم انشاء ملف لهذا الموظف من قبل"});
            }

            TempData["EmployeeId"] = id;

            var employee = await _context.Employees.FindAsync(id);

            var viewModel = new EmployeeHistoryViewModel
            {
                EmployeeId = id,
                EmployeeName = employee?.Name
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> CreateEmployeeHistory(EmployeeHistoryViewModel model,string BranchName,DateTime StartDate,int EmployeeId)
        {

            try 
            {
                var employee = await _context.Employees.FindAsync(EmployeeId);
                if (employee == null)
                {
                    return RedirectToAction("GetAllEmployeeHistories");
                }

                var history = new EmployeeHistory
                {
                    EmployeeId = EmployeeId,
                    Qualification = model.Qualification,
                    HiringDate = model.HiringDate,
                    GraduationYear = model.GraduationYear,
                };
                var Branch = new EmployeeBranches
                {
                    EmployeeId = EmployeeId,
                    BranchName = BranchName,
                    StartDate = StartDate

                };
                await _dbContext.Histories.AddAsync(history);
                await _dbContext.employeeBranches.AddAsync(Branch);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("GetAllEmployeeHistories",new { message = "تم انشاء الملف بنجاح"});
            } 
            catch 
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "حدث خطأ اثناء انشاء الملف" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeHistoryById(int id,string? message = null)
        {
            var history = await _dbContext.Histories
                .Include(h => h.Employee)
                .FirstOrDefaultAsync(h => h.EmployeeId == id);

            if (history == null)
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "لا يوجد ملف لهذا الموظف" });
            }

            var employeeBranches = await _dbContext.employeeBranches
                .Where(b => b.EmployeeId == id)
                .ToListAsync();

            var mappedViewModel = _mapper.Map<EmployeeHistoryViewModel>(history);
            TempData["EmployeeBranches"] = employeeBranches;
            ViewBag.Message = message;
            return View(mappedViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateEmployeeHistory(EmployeeHistoryViewModel model, int EmployeeId)
        {
            var history = await _dbContext.Histories
                .Include(h => h.Employee)
                .FirstOrDefaultAsync(h => h.EmployeeId == EmployeeId);

            if (history == null)
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "لا يوجد ملف لهذا الموظف" });
            }
            history.Qualification = model.Qualification;
            history.HiringDate = model.HiringDate;
            history.GraduationYear = model.GraduationYear;
            history.EndOfServiceDate = model.EndOfServiceDate??null;
            history.EndOfServiceReason = model.EndOfServiceReason ?? "";

            _dbContext.Histories.Update(history);
            int result = await _dbContext.SaveChangesAsync();

            TempData["EmployeeId"] = EmployeeId;
            if (result > 0)
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "تم تحديث الملف بنجاح"  });
            }
            return RedirectToAction("GetAllEmployeeHistories", new { message = "حدث خطأ اثناء تحديث الملف" });
        }

        [HttpPost]
        public async Task<IActionResult> AddBranch(string BranchName,DateTime StartDate , int EmployeeId) 
        {
            try 
            {
                var branch = new EmployeeBranches
                {
                    EmployeeId = EmployeeId,
                    BranchName = BranchName,
                    StartDate = StartDate
                };
                await _dbContext.employeeBranches.AddAsync(branch);
                int result = await _dbContext.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetEmployeeHistoryById", new { id = EmployeeId, message = "تم اضافة الفرع بنجاح" });
                }
                return RedirectToAction("GetEmployeeHistoryById", new { id = EmployeeId, message = "حدث خطأ اثناء اضافة الفرع" });
            } 
            catch 
            {
                return RedirectToAction("GetEmployeeHistoryById", new { id = EmployeeId, message = "حدث خطأ اثناء اضافة الفرع" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiscountsById(int EmployeeId)
        {
            var discounts = await _context.Discounts
                .Where(d => d.MonthlyEmployeeData.EmployeeId == EmployeeId)
                .ToListAsync();
            if (discounts == null || !discounts.Any())
            {
                return RedirectToAction("GetEmployeeHistoryById", new { message = "لا يوجد خصومات لهذا الموظف", id = EmployeeId });
            }
            var mappedDiscounts = _mapper.Map<IEnumerable<DiscountViewModel>>(discounts);
            TempData["EmployeeId"] = EmployeeId;
            TempData.Keep("EmployeeId");
            return View(mappedDiscounts);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBounsById(int EmployeeId)
        {
            var bouns = await _context.Bounss
                .Where(b => b.MonthlyEmployeeData.EmployeeId == EmployeeId)
                .ToListAsync();
            if (bouns == null || !bouns.Any())
            {
                return RedirectToAction("GetEmployeeHistoryById", new { message = "لا يوجد مكافأت لهذا الموظف", id = EmployeeId });
            }
            var mappedBouns = _mapper.Map<IEnumerable<BounsViewModel>>(bouns);
            TempData["EmployeeId"] = EmployeeId;
            TempData.Keep("EmployeeId");
            return View(mappedBouns);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBorrowsById(int EmployeeId)
        {
            var borrows = await _context.Borrows
                .Where(b => b.MonthlyEmployeeData.EmployeeId == EmployeeId)
                .ToListAsync();
            if (borrows == null || !borrows.Any())
            {
                return RedirectToAction("GetEmployeeHistoryById", new { message = "لا يوجد قروض لهذا الموظف", id = EmployeeId });
            }
            var mappedBorrows = _mapper.Map<IEnumerable<BorrowViewModel>>(borrows);
            TempData["EmployeeId"] = EmployeeId;
            TempData.Keep("EmployeeId");
            return View(mappedBorrows);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvaluationsByEmployeeId(int employeeId)
        {
            var evaluations = await _context.quarterlyEvaluations
                .Where(e => e.EmployeeId == employeeId)
                .ToListAsync();
            if (evaluations == null)
            {
                evaluations = new List<QuarterlyEvaluation>();
            }
            TempData["EmployeeId"] = employeeId;

            return View(evaluations);
        }

        [HttpGet]
        public async Task<IActionResult> ViewEvaluationDetails(int evaluationId, int EmployeeId)
        {
            var evaluation = await _context.quarterlyEvaluations
                .Include(q => q.EvaluationResults)
                    .ThenInclude(r => r.EvaluationCriteria)
                .FirstOrDefaultAsync(q => q.Id == evaluationId);

            if (evaluation == null)
                return RedirectToAction("GetAllEvaluationsByEmployeeId", new { employeeId = EmployeeId });

            var viewModel = new QuarterlyEvaluationViewModel
            {
                EmployeeId = evaluation.EmployeeId,
                EmployeeName = evaluation.Employee?.Name ?? "",
                Quarter = evaluation.Quarter,
                EvaluatedBy = evaluation.EvaluatedBy,
                EvaluationResults = evaluation.EvaluationResults.Select(r => new EvaluationResultViewModel
                {
                    EvaluationCriteriaId = r.EvaluationCriteriaId,
                    CriteriaName = r.EvaluationCriteria?.Name ?? "",
                    Rating = r.Rating
                }).ToList()
            };
            TempData["EmployeeId"] = EmployeeId;
            TempData.Keep("EmployeeId");
            return View(viewModel);
        }
    }
}
