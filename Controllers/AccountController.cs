using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Helpers;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

public class AccountController : Controller
{
    private readonly StudentPartTimeJobDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public AccountController(
        StudentPartTimeJobDbContext context,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public IActionResult RegisterStudent()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterStudent(RegisterStudentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Check duplicate email
        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email already exists in the system.");
            return View(model);
        }

        // Fetch Student role
        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
        if (studentRole == null)
        {
            ModelState.AddModelError("", "Role 'Student' not found in database.");
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email.ToLower(),
                PasswordHash = SecurityHelper.HashPassword(model.Password),
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            user.Roles.Add(studentRole);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                UserId = user.UserId,
                University = model.University,
                Major = model.Major,
                GraduationYear = model.GraduationYear,
                CreatedAt = DateTime.Now
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Log Audit
            await _auditService.LogActionAsync(user.UserId, "Register", "User", user.UserId, "Registered new student account");

            TempData["SuccessMessage"] = "Registration successful. Please log in.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "An error occurred while saving your data: " + ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> RegisterEmployer()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterEmployer(RegisterEmployerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName");
            return View(model);
        }

        // Check duplicate email
        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email already exists in the system.");
            ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName");
            return View(model);
        }

        var employerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Employer");
        if (employerRole == null)
        {
            ModelState.AddModelError("", "Role 'Employer' not found in database.");
            ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName");
            return View(model);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create Company
            var company = new Company
            {
                CompanyName = model.CompanyName,
                LogoUrl = model.LogoUrl,
                Website = model.Website,
                Email = model.CompanyEmail,
                Phone = model.CompanyPhone,
                Description = model.Description,
                Address = model.Address,
                ProvinceId = model.ProvinceId,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Create User
            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email.ToLower(),
                PasswordHash = SecurityHelper.HashPassword(model.Password),
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            user.Roles.Add(employerRole);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Employer
            var employer = new Employer
            {
                UserId = user.UserId,
                CompanyId = company.CompanyId,
                Position = model.Position,
                CreatedAt = DateTime.Now
            };

            _context.Employers.Add(employer);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Log Audit
            await _auditService.LogActionAsync(user.UserId, "Register", "User", user.UserId, "Registered new employer account");

            TempData["SuccessMessage"] = "Registration successful. Please log in.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
            ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

        if (user == null || !SecurityHelper.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account has been locked. Please contact admin.");
            return View(model);
        }

        // Create authentication claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        // Log Audit
        await _auditService.LogActionAsync(user.UserId, "Login", "User", user.UserId, "User logged in successfully");

        // Redirect based on role
        if (user.Roles.Any(r => r.RoleName == "Admin"))
            return RedirectToAction("Index", "Admin");

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out int userId))
        {
            await _auditService.LogActionAsync(userId, "Logout", "User", userId, "User logged out");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
        if (user == null)
        {
            // For security, do not disclose that user doesn't exist
            TempData["SuccessMessage"] = "If the email is registered, you will be redirected to reset password.";
            return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
        }

        return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult ResetPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(ForgotPassword));

        return View(new ResetPasswordViewModel { Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
        if (user != null)
        {
            user.PasswordHash = SecurityHelper.HashPassword(model.NewPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(user.UserId, "ResetPassword", "User", user.UserId, "User reset password");
        }

        TempData["SuccessMessage"] = "Password has been reset successfully. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
            return RedirectToAction(nameof(Login));

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return RedirectToAction(nameof(Login));

        if (!SecurityHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
            return View(model);
        }

        user.PasswordHash = SecurityHelper.HashPassword(model.NewPassword);
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(user.UserId, "ChangePassword", "User", user.UserId, "User changed password");

        TempData["SuccessMessage"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
            return RedirectToAction(nameof(Login));

        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Employer).ThenInclude(e => e!.Company)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return RedirectToAction(nameof(Login));

        var model = new ProfileViewModel
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender,
            Address = user.Address
        };

        if (user.Student != null)
        {
            model.University = user.Student.University;
            model.Major = user.Student.Major;
            model.GraduationYear = user.Student.GraduationYear;
            model.Experience = user.Student.Experience;
            model.SkillSummary = user.Student.SkillSummary;
        }
        else if (user.Employer != null)
        {
            model.Position = user.Employer.Position;
            var company = user.Employer.Company;
            if (company != null)
            {
                model.CompanyName = company.CompanyName;
                model.CompanyWebsite = company.Website;
                model.CompanyEmail = company.Email;
                model.CompanyPhone = company.Phone;
                model.CompanyDescription = company.Description;
                model.CompanyAddress = company.Address;
                model.CompanyProvinceId = company.ProvinceId;
            }
        }

        ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName", model.CompanyProvinceId);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName", model.CompanyProvinceId);
            return View(model);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId) || userId != model.UserId)
            return Forbid();

        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Employer).ThenInclude(e => e!.Company)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.AvatarUrl = model.AvatarUrl;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.UpdatedAt = DateTime.Now;

            if (user.Student != null)
            {
                user.Student.University = model.University;
                user.Student.Major = model.Major;
                user.Student.GraduationYear = model.GraduationYear;
                user.Student.Experience = model.Experience;
                user.Student.SkillSummary = model.SkillSummary;
                user.Student.UpdatedAt = DateTime.Now;
            }
            else if (user.Employer != null)
            {
                user.Employer.Position = model.Position;
                user.Employer.UpdatedAt = DateTime.Now;

                var company = user.Employer.Company;
                if (company != null)
                {
                    company.CompanyName = model.CompanyName ?? company.CompanyName;
                    company.Website = model.CompanyWebsite;
                    company.Email = model.CompanyEmail;
                    company.Phone = model.CompanyPhone;
                    company.Description = model.CompanyDescription;
                    company.Address = model.CompanyAddress;
                    company.ProvinceId = model.CompanyProvinceId;
                    company.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogActionAsync(user.UserId, "UpdateProfile", "User", user.UserId, "Updated user profile details");

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Error occurred while saving profile: " + ex.Message);
            ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName", model.CompanyProvinceId);
            return View(model);
        }
    }
}
