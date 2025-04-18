﻿using AutoMapper;
using BLL.Interfaces;
using BLL.Specification;
using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Controllers
{
    [Authorize(Roles = "Manager")]
    public class BranchManager : Controller
    {
        private readonly IMapper _mapper;
        private readonly IGenericRepository<Employee> _empRepo;
        public BranchManager(IMapper mapper, IGenericRepository<Employee> EmpRepo) 
        {
            _mapper = mapper;
            _empRepo = EmpRepo;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var egyptDate = DateOnly.FromDateTime(egyptDateTime);

            var CurrentMonth = egyptDate.Month;
            var CurrentYear =  egyptDate.Year;
            string? BranchName = null;

            var employees = await _empRepo.GetAllWithSpecAsync(new EmployeeSpec(CurrentMonth, CurrentYear, BranchName));
            var MappedEmployess = _mapper.Map<IEnumerable<Employee>, IEnumerable<EmployeeViewModel>>(employees);

            return View(MappedEmployess);
        }
    }
}
