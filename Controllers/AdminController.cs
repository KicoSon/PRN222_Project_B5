using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const int PageSize = 10;

    private readonly StudentPartTimeJobDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICategoryService _categoryService;
    private readonly IJobTypeService _jobTypeService;
    private readonly IProvinceService _provinceService;
    private readonly INotificationService _notificationService;

    public AdminController(
        StudentPartTimeJobDbContext context,
        IAuditService auditService,
        ICategoryService categoryService,
        IJobTypeService jobTypeService,
        IProvinceService provinceService,
        INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _categoryService = categoryService;
        _jobTypeService = jobTypeService;
        _provinceService = provinceService;
        _notificationService = notificationService;
    }

    private int? GetCurrentAdminId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idString, out int id) ? id : (int?)null;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.TotalStudents = await _context.Students.CountAsync();
        ViewBag.TotalEmployers = await _context.Employers.CountAsync();
        ViewBag.TotalCompanies = await _context.Companies.CountAsync();
        ViewBag.TotalJobs = await _context.Jobs.CountAsync(j => j.Status == "Approved");
        ViewBag.TotalJobsAll = await _context.Jobs.CountAsync();
        ViewBag.TotalPending = await _context.Jobs.CountAsync(j => j.Status == "Pending");
        ViewBag.TotalApplications = await _context.Applications.CountAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? search, string? role, int page = 1)
    {
        var query = _context.Users
            .Include(u => u.Roles)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Roles.Any(r => r.RoleName == role));

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.Roles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(int id)
    {
        var user = await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.Student)
            .Include(u => u.Employer).ThenInclude(e => e!.Company)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var model = new UserDetailViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.Roles.Select(r => r.RoleName).ToList(),
            University = user.Student?.University,
            Major = user.Student?.Major,
            Position = user.Employer?.Position,
            CompanyName = user.Employer?.Company?.CompanyName,
            CompanyStatus = user.Employer?.Company?.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUnlock(int userId)
    {
        var currentAdminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(currentAdminIdString, out int adminId) && adminId == userId)
        {
            TempData["ErrorMessage"] = "You cannot lock/unlock your own account.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        string actionName = user.IsActive ? "Unlock Account" : "Lock Account";
        await _auditService.LogActionAsync(adminId, actionName, "User", user.UserId, $"Admin toggled user status. Active: {user.IsActive}");

        TempData["SuccessMessage"] = $"Account for {user.Email} has been {(user.IsActive ? "unlocked" : "locked")} successfully.";
        return RedirectToAction(nameof(Users));
    }

    // =================================================================
    // FEATURE 3.4 - CATEGORY MANAGEMENT
    // =================================================================

    #region Categories

    [HttpGet]
    public async Task<IActionResult> Categories(string? search, int page = 1)
    {
        var (items, totalCount) = await _categoryService.GetPagedAsync(search, page, PageSize);

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        return View(new CategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryViewModel model)
    {
        if (await _categoryService.ExistsByNameAsync(model.CategoryName))
            ModelState.AddModelError(nameof(model.CategoryName), "Tên danh mục đã tồn tại.");

        if (!ModelState.IsValid)
            return View(model);

        var category = await _categoryService.CreateAsync(model);
        await _auditService.LogActionAsync(GetCurrentAdminId(), "Create Category", "Category", category.CategoryId,
            $"Admin created category '{category.CategoryName}'");

        TempData["SuccessMessage"] = $"Đã tạo danh mục '{category.CategoryName}' thành công.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        var model = new CategoryViewModel
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, CategoryViewModel model)
    {
        if (await _categoryService.ExistsByNameAsync(model.CategoryName, id))
            ModelState.AddModelError(nameof(model.CategoryName), "Tên danh mục đã tồn tại.");

        if (!ModelState.IsValid)
            return View(model);

        var success = await _categoryService.UpdateAsync(id, model);
        if (!success)
            return NotFound();

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Update Category", "Category", id,
            $"Admin updated category '{model.CategoryName}'");

        TempData["SuccessMessage"] = $"Đã cập nhật danh mục '{model.CategoryName}' thành công.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryStatus(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
            return NotFound();

        var success = await _categoryService.ToggleStatusAsync(id);
        if (!success)
            return NotFound();

        string actionName = category.IsActive ? "Disable Category" : "Enable Category";
        await _auditService.LogActionAsync(GetCurrentAdminId(), actionName, "Category", id,
            $"Admin toggled category status. Active: {!category.IsActive}");

        TempData["SuccessMessage"] = $"Đã {(category.IsActive ? "vô hiệu hóa" : "kích hoạt")} danh mục '{category.CategoryName}'.";
        return RedirectToAction(nameof(Categories));
    }

    #endregion

    // =================================================================

    #region JobTypes

    [HttpGet]
    public async Task<IActionResult> JobTypes(string? search, int page = 1)
    {
        var (items, totalCount) = await _jobTypeService.GetPagedAsync(search, page, PageSize);

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    [HttpGet]
    public IActionResult CreateJobType()
    {
        return View(new JobTypeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJobType(JobTypeViewModel model)
    {
        if (await _jobTypeService.ExistsByNameAsync(model.TypeName))
            ModelState.AddModelError(nameof(model.TypeName), "Tên loại công việc đã tồn tại.");

        if (!ModelState.IsValid)
            return View(model);

        var jobType = await _jobTypeService.CreateAsync(model);
        await _auditService.LogActionAsync(GetCurrentAdminId(), "Create JobType", "JobType", jobType.JobTypeId,
            $"Admin created job type '{jobType.TypeName}'");

        TempData["SuccessMessage"] = $"Đã tạo loại công việc '{jobType.TypeName}' thành công.";
        return RedirectToAction(nameof(JobTypes));
    }

    [HttpGet]
    public async Task<IActionResult> EditJobType(int id)
    {
        var jobType = await _jobTypeService.GetByIdAsync(id);
        if (jobType == null)
            return NotFound();

        var model = new JobTypeViewModel
        {
            JobTypeId = jobType.JobTypeId,
            TypeName = jobType.TypeName,
            Description = jobType.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditJobType(int id, JobTypeViewModel model)
    {
        if (await _jobTypeService.ExistsByNameAsync(model.TypeName, id))
            ModelState.AddModelError(nameof(model.TypeName), "Tên loại công việc đã tồn tại.");

        if (!ModelState.IsValid)
            return View(model);

        var success = await _jobTypeService.UpdateAsync(id, model);
        if (!success)
            return NotFound();

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Update JobType", "JobType", id,
            $"Admin updated job type '{model.TypeName}'");

        TempData["SuccessMessage"] = $"Đã cập nhật loại công việc '{model.TypeName}' thành công.";
        return RedirectToAction(nameof(JobTypes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleJobTypeStatus(int id)
    {
        var jobType = await _jobTypeService.GetByIdAsync(id);
        if (jobType == null)
            return NotFound();

        var success = await _jobTypeService.ToggleStatusAsync(id);
        if (!success)
            return NotFound();

        string actionName = jobType.IsActive ? "Disable JobType" : "Enable JobType";
        await _auditService.LogActionAsync(GetCurrentAdminId(), actionName, "JobType", id,
            $"Admin toggled job type status. Active: {!jobType.IsActive}");

        TempData["SuccessMessage"] = $"Đã {(jobType.IsActive ? "vô hiệu hóa" : "kích hoạt")} loại công việc '{jobType.TypeName}'.";
        return RedirectToAction(nameof(JobTypes));
    }

    #endregion

    // =================================================================

    #region Provinces

    [HttpGet]
    public async Task<IActionResult> Provinces(string? search, int page = 1)
    {
        var (items, totalCount) = await _provinceService.GetPagedAsync(search, page, PageSize);

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    [HttpGet]
    public IActionResult CreateProvince()
    {
        return View(new ProvinceViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProvince(ProvinceViewModel model)
    {
        if (await _provinceService.ExistsByNameAsync(model.ProvinceName))
            ModelState.AddModelError(nameof(model.ProvinceName), "Tên tỉnh/thành đã tồn tại.");

        if (!ModelState.IsValid)
            return View(model);

        var province = await _provinceService.CreateAsync(model);
        await _auditService.LogActionAsync(GetCurrentAdminId(), "Create Province", "Province", province.ProvinceId,
            $"Admin created province '{province.ProvinceName}'");

        TempData["SuccessMessage"] = $"Đã tạo tỉnh/thành '{province.ProvinceName}' thành công.";
        return RedirectToAction(nameof(Provinces));
    }

    [HttpGet]
    public async Task<IActionResult> EditProvince(int id)
    {
        var province = await _provinceService.GetByIdAsync(id);
        if (province == null)
            return NotFound();

        var model = new ProvinceViewModel
        {
            ProvinceId = province.ProvinceId,
            ProvinceName = province.ProvinceName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProvince(int id, ProvinceViewModel model)
    {
        if (await _provinceService.ExistsByNameAsync(model.ProvinceName, id))
            ModelState.AddModelError(nameof(model.ProvinceName), "Tên tỉnh/thành đã tồn tại.");

        if (!ModelState.IsValid)
            return View(model);

        var success = await _provinceService.UpdateAsync(id, model);
        if (!success)
            return NotFound();

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Update Province", "Province", id,
            $"Admin updated province '{model.ProvinceName}'");

        TempData["SuccessMessage"] = $"Đã cập nhật tỉnh/thành '{model.ProvinceName}' thành công.";
        return RedirectToAction(nameof(Provinces));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProvince(int id)
    {
        var province = await _provinceService.GetByIdAsync(id);
        if (province == null)
            return NotFound();

        var (success, errorMessage) = await _provinceService.DeleteAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Provinces));
        }

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Delete Province", "Province", id,
            $"Admin deleted province '{province.ProvinceName}'");

        TempData["SuccessMessage"] = $"Đã xóa tỉnh/thành '{province.ProvinceName}' thành công.";
        return RedirectToAction(nameof(Provinces));
    }

    #endregion

    // =================================================================
    // FEATURE 3.5 - SYSTEM ADMINISTRATION
    // =================================================================

    #region Notifications

    [HttpGet]
    public async Task<IActionResult> Notifications(int page = 1)
    {
        var (items, totalCount) = await _notificationService.GetPagedAsync(page, PageSize);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> NotificationDetails(int id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
            return NotFound();

        return View(notification);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var success = await _notificationService.MarkAsReadAsync(id);
        if (!success)
            return NotFound();

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Mark Notification Read", "Notification", id,
            "Admin marked notification as read");

        TempData["SuccessMessage"] = "Đã đánh dấu thông báo là đã đọc.";
        return RedirectToAction(nameof(NotificationDetails), new { id });
    }

    #endregion

    // =================================================================

    #region AuditLogs

    [HttpGet]
    public async Task<IActionResult> AuditLogs(int? userId, string? logAction, DateTime? dateFrom, DateTime? dateTo, int page = 1)
    {
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
        {
            TempData["ErrorMessage"] = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
            dateFrom = null;
            dateTo = null;
        }

        var (items, totalCount) = await _auditService.GetPagedAsync(userId, logAction, dateFrom, dateTo, page, PageSize);

        ViewBag.UserId = userId;
        ViewBag.Action = logAction;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.Users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    #endregion
}

