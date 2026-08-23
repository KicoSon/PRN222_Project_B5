using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

public class JobController : Controller
{
    private const int PageSize = 10;

    private readonly StudentPartTimeJobDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public JobController(
        StudentPartTimeJobDbContext context,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    // UC-15: View Job List (public)
    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? categoryId, int? jobTypeId, int? provinceId, decimal? salaryMin, decimal? salaryMax)
    {
        var query = _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.Company)
            .Include(j => j.Category)
            .Include(j => j.JobType)
            .Include(j => j.Province)
            .Where(j => j.Status == "Approved" && j.Deadline >= DateOnly.FromDateTime(DateTime.Today))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(j => j.Title.Contains(keyword) || j.Description.Contains(keyword) || j.Employer.Company.CompanyName.Contains(keyword));

        if (categoryId.HasValue)
            query = query.Where(j => j.CategoryId == categoryId);

        if (jobTypeId.HasValue)
            query = query.Where(j => j.JobTypeId == jobTypeId);

        if (provinceId.HasValue)
            query = query.Where(j => j.ProvinceId == provinceId);

        if (salaryMin.HasValue)
            query = query.Where(j => j.SalaryMax == null || j.SalaryMax >= salaryMin);

        if (salaryMax.HasValue)
            query = query.Where(j => j.SalaryMin == null || j.SalaryMin <= salaryMax);

        if (User.IsInRole("Student"))
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    var appliedJobIds = await _context.Applications
                        .Where(a => a.StudentId == student.StudentId && a.Status != "Rejected")
                        .Select(a => a.JobId)
                        .ToListAsync();

                    query = query.Where(j => !appliedJobIds.Contains(j.JobId));
                }
            }
        }

        var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.JobTypeId = jobTypeId;
        ViewBag.ProvinceId = provinceId;
        ViewBag.SalaryMin = salaryMin;
        ViewBag.SalaryMax = salaryMax;
        ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive == true).OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName", categoryId);
        ViewBag.JobTypes = new SelectList(await _context.JobTypes.Where(t => t.IsActive == true).OrderBy(t => t.TypeName).ToListAsync(), "JobTypeId", "TypeName", jobTypeId);
        ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName", provinceId);

        return View(jobs);
    }

    // UC-16: View Job Detail
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.Company)
            .Include(j => j.Category)
            .Include(j => j.JobType)
            .Include(j => j.Province)
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null)
            return NotFound();

        // For authenticated students - check if already applied
        if (User.IsInRole("Student"))
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    ViewBag.AlreadyApplied = await _context.Applications
                        .AnyAsync(a => a.StudentId == student.StudentId && a.JobId == id);
                    ViewBag.StudentId = student.StudentId;
                    ViewBag.Resumes = await _context.Resumes.Where(r => r.StudentId == student.StudentId).ToListAsync();
                }
            }
        }

        return View(job);
    }

    // UC-11: Create Job (Employer only)
    [HttpGet]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Create()
    {
        await LoadJobFormViewBagsAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Job model)
    {
        // Remove navigation properties from ModelState to avoid validation errors
        ModelState.Remove("Employer");
        ModelState.Remove("Category");
        ModelState.Remove("JobType");
        ModelState.Remove("Province");
        ModelState.Remove("Status");
        ModelState.Remove("RejectReason");
        ModelState.Remove("ApprovedByNavigation");

        if (!ModelState.IsValid)
        {
            await LoadJobFormViewBagsAsync();
            return View(model);
        }

        // Validate business rules
        if (model.SalaryMin.HasValue && model.SalaryMax.HasValue && model.SalaryMax < model.SalaryMin)
        {
            ModelState.AddModelError("SalaryMax", "Maximum salary must be greater than or equal to minimum salary.");
            await LoadJobFormViewBagsAsync();
            return View(model);
        }

        if (model.Deadline < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("Deadline", "Deadline must be a future date.");
            await LoadJobFormViewBagsAsync();
            return View(model);
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null)
        {
            TempData["ErrorMessage"] = "Employer profile not found. Please complete your profile first.";
            return RedirectToAction("Profile", "Account");
        }

        model.EmployerId = employer.EmployerId;
        model.Status = "Pending";
        model.CreatedAt = DateTime.Now;

        _context.Jobs.Add(model);
        await _context.SaveChangesAsync();

        // FEATURE: ONLINE-CV - attach required skills to the newly created job
        if (model.SkillIds != null && model.SkillIds.Count > 0)
        {
            var validSkillIds = await _context.Skills
                .Where(s => s.IsActive && model.SkillIds.Contains(s.SkillId))
                .Select(s => s.SkillId)
                .ToListAsync();
            foreach (var skillId in validSkillIds)
                _context.JobSkills.Add(new JobSkill { JobId = model.JobId, SkillId = skillId });
            await _context.SaveChangesAsync();
        }

        await _auditService.LogActionAsync(userId, "CreateJob", "Job", model.JobId, $"Created job: {model.Title}");

        TempData["SuccessMessage"] = "Job post created and is pending admin approval.";
        return RedirectToAction(nameof(MyJobs));
    }

    // UC-12: Update Job (Employer only)
    [HttpGet]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Edit(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == id && j.EmployerId == employer.EmployerId);
        if (job == null) return NotFound();

        await LoadJobFormViewBagsAsync();
        ViewBag.SelectedSkillIds = await _context.JobSkills.Where(x => x.JobId == job.JobId).Select(x => x.SkillId).ToListAsync(); // FEATURE: ONLINE-CV
        return View(job);
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Job model)
    {
        ModelState.Remove("Employer");
        ModelState.Remove("Category");
        ModelState.Remove("JobType");
        ModelState.Remove("Province");
        ModelState.Remove("ApprovedByNavigation");

        if (!ModelState.IsValid)
        {
            await LoadJobFormViewBagsAsync();
            return View(model);
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == id && j.EmployerId == employer.EmployerId);
        if (job == null) return NotFound();

        // Business rules validation
        if (model.SalaryMin.HasValue && model.SalaryMax.HasValue && model.SalaryMax < model.SalaryMin)
        {
            ModelState.AddModelError("SalaryMax", "Maximum salary must be greater than or equal to minimum salary.");
            await LoadJobFormViewBagsAsync();
            return View(model);
        }

        if (model.Deadline < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("Deadline", "Deadline must be a future date.");
            await LoadJobFormViewBagsAsync();
            return View(model);
        }

        // Detect major content changes -> reset to Pending
        bool majorChange = job.Title != model.Title || job.Description != model.Description ||
                           job.Requirement != model.Requirement || job.CategoryId != model.CategoryId ||
                           job.SalaryMin != model.SalaryMin || job.SalaryMax != model.SalaryMax;

        job.Title = model.Title;
        job.Description = model.Description;
        job.Requirement = model.Requirement;
        job.Benefit = model.Benefit;
        job.SalaryMin = model.SalaryMin;
        job.SalaryMax = model.SalaryMax;
        job.Quantity = model.Quantity;
        job.WorkingTime = model.WorkingTime;
        job.Address = model.Address;
        job.Deadline = model.Deadline;
        job.CategoryId = model.CategoryId;
        job.JobTypeId = model.JobTypeId;
        job.ProvinceId = model.ProvinceId;
        job.UpdatedAt = DateTime.Now;

        if (majorChange && job.Status == "Approved")
            job.Status = "Pending";

        // FEATURE: ONLINE-CV - sync the job's required skills on edit
        var existingSkills = await _context.JobSkills.Where(x => x.JobId == job.JobId).ToListAsync();
        if (existingSkills.Count > 0)
            _context.JobSkills.RemoveRange(existingSkills);
        if (model.SkillIds != null && model.SkillIds.Count > 0)
        {
            var validSkillIds = await _context.Skills
                .Where(s => s.IsActive && model.SkillIds.Contains(s.SkillId))
                .Select(s => s.SkillId)
                .ToListAsync();
            foreach (var skillId in validSkillIds)
                _context.JobSkills.Add(new JobSkill { JobId = job.JobId, SkillId = skillId });
        }

        await _context.SaveChangesAsync();
        await _auditService.LogActionAsync(userId, "UpdateJob", "Job", job.JobId, $"Updated job: {job.Title}");

        TempData["SuccessMessage"] = majorChange && job.Status == "Pending"
            ? "Job updated. It has been re-submitted for admin approval."
            : "Job updated successfully.";
        return RedirectToAction(nameof(MyJobs));
    }

    // UC-13: Delete Job (Employer only)
    [HttpPost]
    [Authorize(Roles = "Employer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var job = await _context.Jobs
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.JobId == id && j.EmployerId == employer.EmployerId);

        if (job == null) return NotFound();

        if (job.Applications.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete job with existing applications. Use 'Close Job' instead.";
            return RedirectToAction(nameof(MyJobs));
        }

        // FEATURE: ONLINE-CV - remove JobSkills join rows first to avoid FK violation
        var joinSkills = await _context.JobSkills.Where(x => x.JobId == job.JobId).ToListAsync();
        if (joinSkills.Count > 0)
            _context.JobSkills.RemoveRange(joinSkills);

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        await _auditService.LogActionAsync(userId, "DeleteJob", "Job", id, $"Deleted job: {job.Title}");

        TempData["SuccessMessage"] = "Job deleted successfully.";
        return RedirectToAction(nameof(MyJobs));
    }

    // UC-14: Close Job (Employer only)
    [HttpPost]
    [Authorize(Roles = "Employer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == id && j.EmployerId == employer.EmployerId);
        if (job == null) return NotFound();

        if (job.Status == "Closed")
        {
            TempData["ErrorMessage"] = "Job is already closed.";
            return RedirectToAction(nameof(MyJobs));
        }

        job.Status = "Closed";
        job.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        await _auditService.LogActionAsync(userId, "CloseJob", "Job", job.JobId, $"Closed job: {job.Title}");

        TempData["SuccessMessage"] = "Job closed successfully.";
        return RedirectToAction(nameof(MyJobs));
    }

    // UC-19: Approve Job (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int adminId)) return Unauthorized();

        var job = await _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(j => j.JobId == id);
        if (job == null) return NotFound();

        job.Status = "Approved";
        job.ApprovedBy = adminId;
        job.ApprovedAt = DateTime.Now;
        job.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(adminId, "ApproveJob", "Job", job.JobId, $"Approved job: {job.Title}");

        // Notify employer
        if (job.Employer?.User != null)
        {
            await _notificationService.CreateNotificationAsync(
                job.Employer.User.UserId,
                "Job Approved",
                $"Your job '{job.Title}' has been approved and is now visible to students.",
                "Job");
        }

        TempData["SuccessMessage"] = $"Job '{job.Title}' approved successfully.";
        return RedirectToAction("PendingJobs");
    }

    // UC-20: Reject Job (Admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int adminId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Rejection reason is required.";
            return RedirectToAction("PendingJobs");
        }

        var job = await _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(j => j.JobId == id);
        if (job == null) return NotFound();

        job.Status = "Rejected";
        job.RejectReason = reason;
        job.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(adminId, "RejectJob", "Job", job.JobId, $"Rejected job: {job.Title}. Reason: {reason}");

        // Notify employer
        if (job.Employer?.User != null)
        {
            await _notificationService.CreateNotificationAsync(
                job.Employer.User.UserId,
                "Job Rejected",
                $"Your job '{job.Title}' was rejected. Reason: {reason}",
                "Job");
        }

        TempData["SuccessMessage"] = $"Job '{job.Title}' rejected.";
        return RedirectToAction("PendingJobs");
    }

    // FEATURE 3.5: Close Job (Admin only) — separate from the Employer-only
    // Close above, since that action checks EmployerId ownership and cannot
    // be reused for Admin moderation.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminClose(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int adminId)) return Unauthorized();

        var job = await _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(j => j.JobId == id);
        if (job == null) return NotFound();

        if (job.Status == "Closed")
        {
            TempData["ErrorMessage"] = "Tin tuyển dụng đã đóng.";
            return RedirectToAction(nameof(PendingJobs));
        }

        job.Status = "Closed";
        job.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(adminId, "AdminCloseJob", "Job", job.JobId, $"Admin closed job: {job.Title}");

        // Notify employer
        if (job.Employer?.User != null)
        {
            await _notificationService.CreateNotificationAsync(
                job.Employer.User.UserId,
                "Job Closed by Admin",
                $"Your job '{job.Title}' has been closed by an administrator.",
                "Job");
        }

        TempData["SuccessMessage"] = $"Đã đóng tin tuyển dụng '{job.Title}'.";
        return RedirectToAction(nameof(PendingJobs));
    }

    // Admin: View / moderate Jobs — FEATURE 3.5 extends this with search,
    // status filter, and pagination. Defaults to "Pending" when no status
    // is specified, preserving the original PendingJobs behavior.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PendingJobs(string? search, string? status, int page = 1)
    {
        var effectiveStatus = string.IsNullOrWhiteSpace(status) ? "Pending" : status;

        var query = _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.Company)
            .Include(j => j.Category)
            .Include(j => j.JobType)
            .Include(j => j.Province)
            .Where(j => j.Status == effectiveStatus)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(j => j.Title.Contains(term) || j.Employer.Company.CompanyName.Contains(term));
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderBy(j => j.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = effectiveStatus;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(jobs);
    }

    // Employer: My Jobs
    [HttpGet]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> MyJobs()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var jobs = await _context.Jobs
            .Include(j => j.Category)
            .Include(j => j.JobType)
            .Include(j => j.Province)
            .Include(j => j.Applications)
            .Where(j => j.EmployerId == employer.EmployerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return View(jobs);
    }

    private async Task LoadJobFormViewBagsAsync()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive == true).OrderBy(c => c.CategoryName).ToListAsync(), "CategoryId", "CategoryName");
        ViewBag.JobTypes = new SelectList(await _context.JobTypes.Where(t => t.IsActive == true).OrderBy(t => t.TypeName).ToListAsync(), "JobTypeId", "TypeName");
        ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.ProvinceName).ToListAsync(), "ProvinceId", "ProvinceName");
        ViewBag.SkillList = await _context.Skills.Where(s => s.IsActive == true).OrderBy(s => s.SkillName).ToListAsync(); // FEATURE: ONLINE-CV
    }
}
