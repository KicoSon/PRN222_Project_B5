using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

/// <summary>
/// FEATURE: ONLINE-CV
/// Student-facing CRUD for creating a CV online from a form. The CV is a
/// normal Resume row (ResumeType = 'Online'), so it can be used directly
/// with the existing application flow via Applications.ResumeId.
/// </summary>
[Authorize(Roles = "Student")]
public class OnlineCvController : Controller
{
    private readonly StudentPartTimeJobDbContext _context;
    private readonly IAuditService _auditService;

    public OnlineCvController(StudentPartTimeJobDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // List the student's online CVs.
    public async Task<IActionResult> Index()
    {
        var (userId, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        var cvs = await _context.Resumes
            .Where(r => r.StudentId == student.StudentId && r.ResumeType == "Online")
            .OrderByDescending(r => r.IsDefault)
            .ThenByDescending(r => r.UploadedAt)
            .ToListAsync();

        return View(cvs);
    }

    // Create - GET
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadSkillViewsAsync(null);
        return View(new OnlineCvViewModel());
    }

    // Create - POST (PRG: redirect to Preview after save)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OnlineCvViewModel model, int[]? skillIds)
    {
        model.SelectedSkillIds = skillIds?.ToList() ?? new List<int>();

        if (!ModelState.IsValid)
        {
            await LoadSkillViewsAsync(model.SelectedSkillIds);
            return View(model);
        }

        var (userId, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        bool hasDefault = await _context.Resumes
            .AnyAsync(r => r.StudentId == student.StudentId && r.IsDefault);

        var resume = new Resume
        {
            StudentId = student.StudentId,
            FileName = $"CV trực tuyến {DateTime.Now:yyyy-MM-dd}",
            FilePath = string.Empty,
            ContentType = "online",
            ResumeType = "Online",
            DesiredTitle = model.DesiredTitle,
            CareerObjective = model.CareerObjective,
            Education = model.Education,
            WorkExperience = model.WorkExperience,
            Projects = model.Projects,
            Certifications = model.Certifications,
            IsDefault = !hasDefault,
            UploadedAt = DateTime.Now
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();

        await ReplaceResumeSkillsAsync(resume.ResumeId, model.SelectedSkillIds);

        await _auditService.LogActionAsync(userId, "CreateOnlineCv", "Resume", resume.ResumeId, $"Created online CV: {resume.DesiredTitle}");
        TempData["SuccessMessage"] = "CV trực tuyến đã được tạo thành công.";
        return RedirectToAction(nameof(Preview), new { id = resume.ResumeId });
    }

    // Edit - GET
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (_, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        var resume = await _context.Resumes
            .Include(r => r.ResumeSkills)
            .FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId && r.ResumeType == "Online");
        if (resume == null) return NotFound();

        var model = new OnlineCvViewModel
        {
            ResumeId = resume.ResumeId,
            DesiredTitle = resume.DesiredTitle ?? string.Empty,
            CareerObjective = resume.CareerObjective ?? string.Empty,
            Education = resume.Education,
            WorkExperience = resume.WorkExperience,
            Projects = resume.Projects,
            Certifications = resume.Certifications,
            SelectedSkillIds = resume.ResumeSkills.Select(rs => rs.SkillId).ToList()
        };

        await LoadSkillViewsAsync(model.SelectedSkillIds);
        return View(model);
    }

    // Edit - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OnlineCvViewModel model, int[]? skillIds)
    {
        model.SelectedSkillIds = skillIds?.ToList() ?? new List<int>();

        if (id != model.ResumeId)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await LoadSkillViewsAsync(model.SelectedSkillIds);
            return View(model);
        }

        var (userId, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId && r.ResumeType == "Online");
        if (resume == null) return NotFound();

        resume.DesiredTitle = model.DesiredTitle;
        resume.CareerObjective = model.CareerObjective;
        resume.Education = model.Education;
        resume.WorkExperience = model.WorkExperience;
        resume.Projects = model.Projects;
        resume.Certifications = model.Certifications;
        await _context.SaveChangesAsync();

        await ReplaceResumeSkillsAsync(resume.ResumeId, model.SelectedSkillIds);

        await _auditService.LogActionAsync(userId, "UpdateOnlineCv", "Resume", resume.ResumeId, $"Edited online CV: {resume.DesiredTitle}");
        TempData["SuccessMessage"] = "CV trực tuyến đã được cập nhật.";
        return RedirectToAction(nameof(Preview), new { id = resume.ResumeId });
    }

    // Preview - show the full CV + print button
    public async Task<IActionResult> Preview(int id)
    {
        var (_, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        var resume = await _context.Resumes
            .Include(r => r.ResumeSkills).ThenInclude(rs => rs.Skill)
            .FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId && r.ResumeType == "Online");
        if (resume == null) return NotFound();

        return View(resume);
    }
// Make this online CV the student's default (clears all others).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeDefault(int id)
    {
        var (userId, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId && r.ResumeType == "Online");
        if (resume == null) return NotFound();

        var allResumes = await _context.Resumes
            .Where(r => r.StudentId == student.StudentId)
            .ToListAsync();
        foreach (var r in allResumes)
            r.IsDefault = (r.ResumeId == id);

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(userId, "MakeDefaultOnlineCv", "Resume", id, "Set online CV as default");
        TempData["SuccessMessage"] = "Đã đặt CV trực tuyến làm CV mặc định.";
        return RedirectToAction(nameof(Index));
    }

    // Delete an online CV (and its skills), if not referenced by applications.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, student) = await GetStudentAsync();
        if (student == null) return Unauthorized();

        var resume = await _context.Resumes
            .Include(r => r.Applications)
            .FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId && r.ResumeType == "Online");
        if (resume == null) return NotFound();

        if (resume.Applications.Any())
        {
            TempData["ErrorMessage"] = "Không thể xóa CV đang được sử dụng trong đơn ứng tuyển.";
            return RedirectToAction(nameof(Index));
        }

        // remove the join rows first to avoid FK violation (the physical DB
        // FK was created without CASCADE).
        var skills = await _context.ResumeSkills.Where(rs => rs.ResumeId == id).ToListAsync();
        if (skills.Count > 0)
            _context.ResumeSkills.RemoveRange(skills);

        _context.Resumes.Remove(resume);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(userId, "DeleteOnlineCv", "Resume", id, "Deleted online CV");
        TempData["SuccessMessage"] = "CV trực tuyến đã được xóa.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- helpers ----------------

    private async Task<(int? UserId, Student? Student)> GetStudentAsync()
    {
        var str = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(str, out int id))
            return (null, null);
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == id);
        return (id, student);
    }

    private async Task LoadSkillViewsAsync(IEnumerable<int>? selected)
    {
        var selectedSet = selected == null ? new HashSet<int>() : new HashSet<int>(selected);
        ViewBag.SkillList = await _context.Skills.Where(s => s.IsActive).OrderBy(s => s.SkillName).ToListAsync();
        ViewBag.SelectedSkillIds = selectedSet;
    }

    private async Task ReplaceResumeSkillsAsync(int resumeId, ICollection<int> skillIds)
    {
        var validIds = await _context.Skills
            .Where(s => s.IsActive && skillIds.Contains(s.SkillId))
            .Select(s => s.SkillId)
            .ToListAsync();

        var existing = await _context.ResumeSkills.Where(rs => rs.ResumeId == resumeId).ToListAsync();
        if (existing.Count > 0)
            _context.ResumeSkills.RemoveRange(existing);

        foreach (var skillId in validIds)
            _context.ResumeSkills.Add(new ResumeSkill { ResumeId = resumeId, SkillId = skillId });

        await _context.SaveChangesAsync();
    }
}