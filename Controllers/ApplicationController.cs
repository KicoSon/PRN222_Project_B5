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

[Authorize]
public class ApplicationController : Controller
{
    private readonly StudentPartTimeJobDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public ApplicationController(
        StudentPartTimeJobDbContext context,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    // UC-21: Apply for a Job (Student only)
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(int jobId, int? resumeId, string? coverLetter)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Unauthorized();

        var job = await _context.Jobs
            .Include(j => j.Employer).ThenInclude(e => e.User)
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.Status == "Approved");

        if (job == null)
        {
            TempData["ErrorMessage"] = "Job not found or not open for applications.";
            return RedirectToAction("Index", "Job");
        }

        // Check deadline
        if (job.Deadline < DateOnly.FromDateTime(DateTime.Today))
        {
            TempData["ErrorMessage"] = "This job's deadline has passed.";
            return RedirectToAction("Details", "Job", new { id = jobId });
        }

        // Prevent duplicate applications
        var alreadyApplied = await _context.Applications
            .AnyAsync(a => a.StudentId == student.StudentId && a.JobId == jobId);

        if (alreadyApplied)
        {
            TempData["ErrorMessage"] = "You have already applied for this job.";
            return RedirectToAction("Details", "Job", new { id = jobId });
        }

        // Validate resume belongs to student if specified
        if (resumeId.HasValue)
        {
            var resumeValid = await _context.Resumes
                .AnyAsync(r => r.ResumeId == resumeId && r.StudentId == student.StudentId);
            if (!resumeValid)
            {
                TempData["ErrorMessage"] = "Selected resume is invalid.";
                return RedirectToAction("Details", "Job", new { id = jobId });
            }
        }

        var application = new Application
        {
            StudentId = student.StudentId,
            JobId = jobId,
            ResumeId = resumeId,
            CoverLetter = coverLetter,
            Status = "Pending",
            AppliedAt = DateTime.Now
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(userId, "ApplyJob", "Application", application.ApplicationId, $"Applied to job {jobId}");

        // Notify employer
        if (job.Employer?.User != null)
        {
            await _notificationService.CreateNotificationAsync(
                job.Employer.User.UserId,
                "New Application Received",
                $"A student has applied for your job '{job.Title}'.",
                "Application");
        }

        TempData["SuccessMessage"] = "Application submitted successfully!";
        return RedirectToAction(nameof(History));
    }

    // UC-22: Cancel Application (Student only)
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Unauthorized();

        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == student.StudentId);

        if (application == null) return NotFound();

        if (application.Status != "Pending")
        {
            TempData["ErrorMessage"] = "Only pending applications can be cancelled.";
            return RedirectToAction(nameof(History));
        }

        application.Status = "Cancelled";
        application.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(userId, "CancelApplication", "Application", id, "Cancelled application");

        TempData["SuccessMessage"] = "Application cancelled.";
        return RedirectToAction(nameof(History));
    }

    // UC-23: View Application History (Student)
    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> History()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return Unauthorized();

        var applications = await _context.Applications
            .Include(a => a.Job).ThenInclude(j => j.Employer).ThenInclude(e => e.Company)
            .Include(a => a.Job).ThenInclude(j => j.Province)
            .Include(a => a.Resume)
            .Where(a => a.StudentId == student.StudentId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        return View(applications);
    }

    // UC-24: View Applicants for a Job (Employer)
    [HttpGet]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Applicants(int jobId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId && j.EmployerId == employer.EmployerId);
        if (job == null) return NotFound();

        var applications = await _context.Applications
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Resume)
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        ViewBag.Job = job;
        return View(applications);
    }

    // UC-25: View/Download Applicant Resume (Employer)
    [HttpGet]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Resume(int resumeId, int jobId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        // Ensure the application belongs to an employer's job
        var applicationExists = await _context.Applications
            .AnyAsync(a => a.ResumeId == resumeId && a.Job.EmployerId == employer.EmployerId && a.JobId == jobId);

        if (!applicationExists) return Forbid();

        var resume = await _context.Resumes.FindAsync(resumeId);
        if (resume == null) return NotFound();

        // Redirect to the static file path (protected by auth above)
        return Redirect(resume.FilePath);
    }

    // UC-26: Update Application Status (Employer)
    [HttpPost]
    [Authorize(Roles = "Employer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? note)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var employer = await _context.Employers.FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null) return Unauthorized();

        var validStatuses = new[] { "Interview", "Approved", "Rejected" };
        if (!validStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Invalid status.";
            return RedirectToAction(nameof(Applicants));
        }

        var application = await _context.Applications
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == id && a.Job.EmployerId == employer.EmployerId);

        if (application == null) return NotFound();

        application.Status = status;
        application.EmployerNote = note;
        application.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(userId, "UpdateApplicationStatus", "Application", id, $"Status updated to {status}");

        // Notify student
        if (application.Student?.User != null)
        {
            await _notificationService.CreateNotificationAsync(
                application.Student.User.UserId,
                "Application Status Updated",
                $"Your application for '{application.Job.Title}' has been updated to: {status}.",
                "Application");
        }

        TempData["SuccessMessage"] = $"Application status updated to {status}.";
        return RedirectToAction(nameof(Applicants), new { jobId = application.JobId });
    }

    private int? GetCurrentUserId()
    {
        var str = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(str, out int id)) return id;
        return null;
    }
}
