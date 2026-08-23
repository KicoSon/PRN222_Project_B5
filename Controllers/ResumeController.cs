using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

[Authorize(Roles = "Student")]
public class ResumeController : Controller
{
    private readonly StudentPartTimeJobDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IWebHostEnvironment _env;
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public ResumeController(StudentPartTimeJobDbContext context, IAuditService auditService, IWebHostEnvironment env)
    {
        _context = context;
        _auditService = auditService;
        _env = env;
    }

    // UC-09: Manage Resume - list
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return NotFound();

        var resumes = await _context.Resumes
            .Where(r => r.StudentId == student.StudentId)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();

        return View(resumes);
    }

    // UC-08: Upload Resume
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return NotFound();

        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            TempData["ErrorMessage"] = $"Only PDF, DOC, DOCX files are allowed. You uploaded: {ext}";
            return RedirectToAction(nameof(Index));
        }

        if (file.Length > MaxFileSizeBytes)
        {
            TempData["ErrorMessage"] = "File size must not exceed 5 MB.";
            return RedirectToAction(nameof(Index));
        }

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "resumes");
        Directory.CreateDirectory(uploadDir);

        var safeFileName = $"{student.StudentId}_{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, safeFileName);
        var relPath = $"/uploads/resumes/{safeFileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        bool hasDefault = await _context.Resumes.AnyAsync(r => r.StudentId == student.StudentId && r.IsDefault);

        var resume = new Resume
        {
            StudentId = student.StudentId,
            FileName = file.FileName,
            FilePath = relPath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            IsDefault = !hasDefault, // first upload is auto default
            UploadedAt = DateTime.Now
        };

        _context.Resumes.Add(resume);
        await _context.SaveChangesAsync();
        await _auditService.LogActionAsync(userId, "UploadResume", "Resume", resume.ResumeId, $"Uploaded resume: {file.FileName}");

        TempData["SuccessMessage"] = "Resume uploaded successfully.";
        return RedirectToAction(nameof(Index));
    }

    // UC-09: Set Default Resume
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return NotFound();

        var resume = await _context.Resumes.FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId);
        if (resume == null) return NotFound();

        // Clear all defaults
        var allResumes = await _context.Resumes.Where(r => r.StudentId == student.StudentId).ToListAsync();
        foreach (var r in allResumes)
            r.IsDefault = (r.ResumeId == id);

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Default resume updated.";
        return RedirectToAction(nameof(Index));
    }

    // UC-09: Delete Resume
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return NotFound();

        var resume = await _context.Resumes
            .Include(r => r.Applications)
            .FirstOrDefaultAsync(r => r.ResumeId == id && r.StudentId == student.StudentId);

        if (resume == null) return NotFound();

        if (resume.Applications.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete resume that is used in existing applications.";
            return RedirectToAction(nameof(Index));
        }

        // Delete physical file
        if (!string.IsNullOrEmpty(resume.FilePath))
        {
            var physicalPath = Path.Combine(_env.WebRootPath, resume.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);
        }

        // FEATURE: ONLINE-CV - remove online CV skill join rows first to avoid FK violation
        var joinSkills = await _context.ResumeSkills.Where(rs => rs.ResumeId == id).ToListAsync();
        if (joinSkills.Count > 0)
            _context.ResumeSkills.RemoveRange(joinSkills);

        _context.Resumes.Remove(resume);
        await _context.SaveChangesAsync();
        await _auditService.LogActionAsync(userId, "DeleteResume", "Resume", id, "Deleted resume");

        TempData["SuccessMessage"] = "Resume deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var str = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(str, out int id)) return id;
        return null;
    }
}
