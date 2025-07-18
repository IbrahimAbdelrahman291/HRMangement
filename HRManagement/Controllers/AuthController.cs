﻿using DAL.Models;
using HRManagement.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager , SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public IActionResult Index(string? message)
        {
            ViewBag.Message = message;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                return RedirectToAction("Index","Auth");
            }

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return RedirectToAction("Index", new { message = "هذا المستخدم غير موجود بالسيستم" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { message = "هناك خطأ في كلمه المرور او اسم المستخدم" });
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (await _userManager.IsInRoleAsync(user, "HR"))
            {
                return RedirectToAction("Index", "HR");
            }
            else if (await _userManager.IsInRoleAsync(user, "Employee"))
            {
                var userId = user.Id;
                return RedirectToAction("Index", "Employee", new { userId = userId });
            }
            else if (await _userManager.IsInRoleAsync(user, "Accountant"))
            {
                return RedirectToAction("Index", "Accountant");
            }
            else if (await _userManager.IsInRoleAsync(user, "Manager"))
            {
                return RedirectToAction("Index", "BranchManager");
            }
            else if (await _userManager.IsInRoleAsync(user, "Control"))
            {
                return RedirectToAction("Index", "Control");
            }

            return RedirectToAction("Index", new { message = "هناك خطأ في كلمه المرور او اسم المستخدم" });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }
    }
}
