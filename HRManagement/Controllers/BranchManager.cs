using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Context;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "Manager")]
    public class BranchManager : Controller
    {
        private readonly IMapper _mapper;
        private readonly IGenericRepository<Employee> _empRepo;
        private readonly HRDbContext _context;

        public BranchManager(IMapper mapper, IGenericRepository<Employee> EmpRepo, HRDbContext context)
        {
            _mapper = mapper;
            _empRepo = EmpRepo;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees(int? month = null, int? year = null, string? BranchName = null, string? Role = null, string? BankName = null, string? Name = null)
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


        [HttpGet]
        public async Task<IActionResult> GetAllEmployeeHistories(string? searchName, int? searchId, string? message = null)
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
        public async Task<IActionResult> GetEmployeeHistoryById(int id, string? message = null)
        {
            var history = await _context.Histories
                .Include(h => h.Employee)
                .FirstOrDefaultAsync(h => h.EmployeeId == id);

            if (history == null)
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "لا يوجد ملف لهذا الموظف" });
            }

            var employeeBranches = await _context.employeeBranches
                .Where(b => b.EmployeeId == id)
                .ToListAsync();

            var mappedViewModel = _mapper.Map<EmployeeHistoryViewModel>(history);
            TempData["EmployeeBranches"] = employeeBranches;
            TempData["EmployeeId"] = id;
            ViewBag.Message = message;
            return View(mappedViewModel);
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
        public async Task<IActionResult> CreateEvaluation(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return RedirectToAction("GetAllEmployeeHistories");

            var criteriaList = await _context.evaluationCriterias.ToListAsync();

            var model = new QuarterlyEvaluationViewModel
            {
                EmployeeId = employeeId,
                EmployeeName = employee.Name,
                Quarter = "", // هيتكتب من الفورم
                EvaluatedBy = "", // HR يكتبه أو يتجاب من الـ User.Identity.Name
                EvaluationResults = criteriaList.Select(c => new EvaluationResultViewModel
                {
                    EvaluationCriteriaId = c.Id,
                    CriteriaName = c.Name
                }).ToList()
            };
            TempData["EmployeeId"] = employeeId;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvaluation(QuarterlyEvaluationViewModel model)
        {
            try
            {

                // إنشاء تقييم ربع سنوي جديد
                var evaluation = new QuarterlyEvaluation
                {
                    EmployeeId = model.EmployeeId,
                    Quarter = model.Quarter,
                    EvaluatedBy = model.EvaluatedBy,
                    EvaluationResults = new List<EvaluationResult>()
                };

                // إضافة النتائج لكل بند تقييم
                foreach (var result in model.EvaluationResults)
                {
                    evaluation.EvaluationResults.Add(new EvaluationResult
                    {
                        EvaluationCriteriaId = result.EvaluationCriteriaId,
                        Rating = result.Rating
                    });
                }

                // الحفظ في قاعدة البيانات
                await _context.quarterlyEvaluations.AddAsync(evaluation);
                await _context.SaveChangesAsync();

                return RedirectToAction("GetAllEvaluationsByEmployeeId", new { EmployeeId = model.EmployeeId });
            }
            catch
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "حدث خطأ أثناء إنشاء التقييم" });
            }
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
        [HttpPost]
        public async Task<IActionResult> UpdateEvaluation(QuarterlyEvaluationViewModel model, int EmployeeId)
        {

            try
            {
                var evaluation = await _context.quarterlyEvaluations
               .Include(e => e.EvaluationResults)
               .FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);

                if (evaluation == null)
                {
                    return RedirectToAction("GetAllEvaluationsByEmployeeId", new { employeeId = EmployeeId });
                }

                // تحديث البيانات الأساسية
                evaluation.Quarter = model.Quarter;
                evaluation.EvaluatedBy = model.EvaluatedBy;

                // تحديث النتائج المرتبطة
                foreach (var resultVM in model.EvaluationResults)
                {
                    var existingResult = evaluation.EvaluationResults
                        .FirstOrDefault(r => r.EvaluationCriteriaId == resultVM.EvaluationCriteriaId);

                    if (existingResult != null)
                    {
                        existingResult.Rating = resultVM.Rating;
                    }
                }

                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return RedirectToAction("GetAllEvaluationsByEmployeeId", new { employeeId = EmployeeId });
                }
                return RedirectToAction("GetAllEmployeeHistories", new { message = "حدث خطأ أثناء تحديث التقييم" });

            }
            catch
            {
                return RedirectToAction("GetAllEmployeeHistories", new { message = "حدث خطأ أثناء تحديث التقييم" });
            }

        }
    }
}
