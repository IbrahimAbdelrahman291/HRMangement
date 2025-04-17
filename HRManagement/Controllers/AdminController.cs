using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IGenericRepository<Employee> _empRepo;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(IMapper mapper,IGenericRepository<Employee> EmpRepo,UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _mapper = mapper;
            _empRepo = EmpRepo;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees(int? month = null, int? year = null,string? BranchName = null,string? message = null)
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var currentMonth = month ?? egyptDate.Month;
            var currentYear = year ?? egyptDate.Year;


            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(currentMonth, currentYear, BranchName));
            var mappedEmployees = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees);
            ViewBag.Message = message;
            return View(mappedEmployees);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteEmployee([FromForm] int id) 
        {
            var Employee = await _empRepo.GetByIdWithSpecAsync(new EmployeeSpec(id));
            var UserId = Employee.UserId;
            _empRepo.Delete(Employee);
            var user = await _userManager.FindByIdAsync(UserId);
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("GetAllEmployees" , new { message = "تم مسح الموظف بنجاح"});

            }
            else
            {
                return RedirectToAction("GetAllEmployees", new { message = "حدث خطأ ولم يتم مسح الموظف"});
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllHR(string? message = null)
        {
            var hrUsers = await _userManager.GetUsersInRoleAsync("HR");
            ViewBag.Message = message;
            return View(hrUsers);
        }
        [HttpGet]
        public IActionResult AddHR() 
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddHR(string userName, string password)
        {
            userName = userName.Trim().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return RedirectToAction("GetAllHR", new { message = "لم يتم اضافه اليوزر بنجاح قد يكون هناك مسافات او قيم فارغه" });
            }

            if (!await _roleManager.RoleExistsAsync("HR"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("HR"));
                if (!roleResult.Succeeded)
                {
                    return RedirectToAction("GetAllHR", new { message = "حدث خطأ في انشاء الدور"});
                }
            }

            var existingUser = await _userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                return RedirectToAction("GetAllHR",new { message = "المستخدم موجود بالفعل" });
            }

            var user = new User
            {
                UserName = userName
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return RedirectToAction("GetAllHR", new { message = "حدث خطأ في انشاء المستخدم" });
            }

            var createdUser = await _userManager.FindByNameAsync(user.UserName);
            if (createdUser == null)
            {
                return RedirectToAction("GetAllHR",new { message = "المستخدم المضاف تم فقضه ولم نستطع اضافه الدور له" });
            }

            var roleAssignResult = await _userManager.AddToRoleAsync(createdUser, "HR");
            if (!roleAssignResult.Succeeded)
            {
                return RedirectToAction("GetAllHR", new { message = "لم نستطع اضافه المستخدم للدور المنسوب اليه"});
            }

            return RedirectToAction("GetAllHR", new { message = "تم اضافه المستخدم وادواره بنجاح" });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteHR(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("GetAllHR",new { message = "لم يتم العثور على المستخدم" });
            }

            var isHR = await _userManager.IsInRoleAsync(user, "HR");
            if (!isHR)
            {
                return RedirectToAction("GetAllHR", new { message = "هذا المستخدم دوره ليس HR" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("GetAllHR", new { message = "تم مسح المستخدم بنجاح" });
            }

            return RedirectToAction("GetAllHR", new { message = "حدث خطأ غير معروف" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAccountant(string? message =null ) 
        {
            var AccountantUsers = await _userManager.GetUsersInRoleAsync("Accountant");
            ViewBag.Message = message;
            return View(AccountantUsers);
        }

        [HttpGet]
        public IActionResult AddAccountant()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddAccountant(string userName,string Password) 
        {
            userName = userName.Trim().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(Password))
            {
                return RedirectToAction("GetAllAccountant", new { message = "لم يتم اضافه المستخدم بنجاح قد يكون هناك مسافات او قيم فارغه" });
            }

            if (!await _roleManager.RoleExistsAsync("Accountant"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Accountant"));
                if (!roleResult.Succeeded)
                {
                    return RedirectToAction("GetAllAccountant", new { message = "حدث خطأ في انشاء الدور" });
                }
            }

            var existingUser = await _userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                return RedirectToAction("GetAllAccountant", new { message = "المستخدم موجود بالفعل" });
            }

            var user = new User
            {
                UserName = userName
            };

            var result = await _userManager.CreateAsync(user, Password);
            if (!result.Succeeded)
            {
                return RedirectToAction("GetAllAccountant", new { message = "حدث خطأ في انشاء المستخدم" });
            }

            var createdUser = await _userManager.FindByNameAsync(user.UserName);
            if (createdUser == null)
            {
                return RedirectToAction("GetAllAccountant", new { message = "المستخدم المضاف تم فقضه ولم نستطع اضافه الدور له" });
            }

            var roleAssignResult = await _userManager.AddToRoleAsync(createdUser, "Accountant");
            if (!roleAssignResult.Succeeded)
            {
                return RedirectToAction("GetAllAccountant", new { message = "لم نستطع اضافه المستخدم للدور المنسوب اليه" });
            }

            return RedirectToAction("GetAllAccountant", new { message = "تم اضافه المستخدم وادواره بنجاح" });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteAccountant(string id) 
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("GetAllAccountant", new { message = "لم يتم العثور على المستخدم" });
            }

            var isAccountant = await _userManager.IsInRoleAsync(user, "Accountant");
            if (!isAccountant)
            {
                return RedirectToAction("GetAllAccountant", new { message = "هذا المستخدم دوره ليس محاسب" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("GetAllAccountant", new { message = "تم مسح المستخدم بنجاح" });
            }

            return RedirectToAction("GetAllAccountant", new { message = "حدث خطأ غير معروف" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBranchesManager(string? message = null)
        {
            var ManagerUsers = await _userManager.GetUsersInRoleAsync("Manager");
            ViewBag.Message = message;
            return View(ManagerUsers);
        }
        [HttpGet]
        public IActionResult AddBranchesManager()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddBranchesManager(string userName,string Password) 
        {
            userName = userName.Trim().Replace(" ", "");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(Password))
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "لم يتم اضافه المستخدم بنجاح قد يكون هناك مسافات او قيم فارغه" });
            }

            if (!await _roleManager.RoleExistsAsync("Manager"))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole("Manager"));
                if (!roleResult.Succeeded)
                {
                    return RedirectToAction("GetAllBranchesManager", new { message = "حدث خطأ في انشاء الدور" });
                }
            }

            var existingUser = await _userManager.FindByNameAsync(userName);
            if (existingUser != null)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "المستخدم موجود بالفعل" });
            }

            var user = new User
            {
                UserName = userName
            };

            var result = await _userManager.CreateAsync(user, Password);
            if (!result.Succeeded)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "حدث خطأ في انشاء المستخدم" });
            }

            var createdUser = await _userManager.FindByNameAsync(user.UserName);
            if (createdUser == null)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "المستخدم المضاف تم فقضه ولم نستطع اضافه الدور له" });
            }

            var roleAssignResult = await _userManager.AddToRoleAsync(createdUser, "Manager");
            if (!roleAssignResult.Succeeded)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "لم نستطع اضافه المستخدم للدور المنسوب اليه" });
            }

            return RedirectToAction("GetAllBranchesManager", new { message = "تم اضافه المستخدم وادواره بنجاح" });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBracheManager(string id) 
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "لم يتم العثور على المستخدم" });
            }

            var isAccountant = await _userManager.IsInRoleAsync(user, "Manager");
            if (!isAccountant)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "هذا المستخدم دوره ليس مدير فرع" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("GetAllBranchesManager", new { message = "تم مسح المستخدم بنجاح" });
            }

            return RedirectToAction("GetAllBranchesManager", new { message = "حدث خطأ غير معروف" });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers(string? userName = null,string? message = null)
        {
            var users = await _userManager.Users.ToListAsync();
            if(userName != null) 
            {
                users = users.Where(u => u.UserName.Contains(userName)).ToList();
            }
            ViewBag.Message = message;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userName) 
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return RedirectToAction("GetAllUsers", new { message = "لم يتم العثور على المستخدم" });
            }
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userName, string newPassword)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(userName);
                if (user == null)
                {
                    return RedirectToAction("GetAllUsers", new { message = "لم يتم العثور على المستخدم" });
                }

                await _userManager.RemovePasswordAsync(user);

                var result = await _userManager.AddPasswordAsync(user, newPassword);
                if (result.Succeeded)
                {
                    return RedirectToAction("GetAllUsers", new { message = "تم اعاده تعيين كلمه المرور بنجاح" });
                }
                else
                {
                    return RedirectToAction("GetAllUsers", new { message = "حدث خطأ في اعاده تعيين كلمه المرور" });
                }
            }
            return View(userName);

        }
    }
}

