using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

// =====================================================================
// FEATURE: ONLINE-CV (service - keeps the matching logic out of controllers)
// =====================================================================
public interface IJobRecommendationService
{
    Task<List<JobRecommendationViewModel>> GetRecommendedJobsAsync(int userId, int topCount = 6);
}

public class JobRecommendationService : IJobRecommendationService
{
    private readonly StudentPartTimeJobDbContext _context;

    public JobRecommendationService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobRecommendationViewModel>> GetRecommendedJobsAsync(int userId, int topCount = 6)
    {
        var result = new List<JobRecommendationViewModel>();

        // 1. Find the student record for the current user.
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null)
            return result;

        // 2. Use the default online CV; fall back to the most recent one.
        var cv = await _context.Resumes
            .FirstOrDefaultAsync(r => r.StudentId == student.StudentId && r.ResumeType == "Online" && r.IsDefault);
        cv ??= await _context.Resumes
            .Where(r => r.StudentId == student.StudentId && r.ResumeType == "Online")
            .OrderByDescending(r => r.UploadedAt)
            .FirstOrDefaultAsync();

        if (cv == null)
            return result;

        // 3. The student's skill set.
        var mySkillIds = await _context.ResumeSkills
            .Where(rs => rs.ResumeId == cv.ResumeId)
            .Select(rs => rs.SkillId)
            .ToListAsync();

        if (mySkillIds.Count == 0)
            return result;

        var mySkillSet = new HashSet<int>(mySkillIds);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 4. Valid jobs: Approved, not past deadline and carrying >=1 skill.
        var jobs = await _context.Jobs
            .Include(j => j.JobSkills).ThenInclude(js => js.Skill)
            .Include(j => j.Employer).ThenInclude(e => e.Company)
            .Include(j => j.Province)
            .Include(j => j.JobType)
            .Where(j => j.Status == "Approved"
                        && j.Deadline >= today
                        && j.JobSkills.Any())
            .ToListAsync();

        foreach (var job in jobs)
        {
            var requiredSkillIds = job.JobSkills.Select(js => js.SkillId).Distinct().ToList();
            var matchedIds = requiredSkillIds.Where(id => mySkillSet.Contains(id)).ToList();

            // keep only jobs with at least one overlapping skill
            if (matchedIds.Count == 0)
                continue;

            var matchedCount = matchedIds.Count;
            var percent = requiredSkillIds.Count == 0
                ? 0
                : (int)Math.Round((double)matchedCount / requiredSkillIds.Count * 100);

            var matchedSkillNames = job.JobSkills
                .Where(js => mySkillSet.Contains(js.SkillId))
                .Select(js => js.Skill.SkillName)
                .Distinct()
                .ToList();
            var missingSkillNames = job.JobSkills
                .Where(js => !mySkillSet.Contains(js.SkillId))
                .Select(js => js.Skill.SkillName)
                .Distinct()
                .ToList();

            result.Add(new JobRecommendationViewModel
            {
                JobId = job.JobId,
                Title = job.Title,
                CompanyName = job.Employer?.Company?.CompanyName,
                ProvinceName = job.Province?.ProvinceName,
                JobTypeName = job.JobType?.TypeName,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Deadline = job.Deadline,
                MatchPercent = percent,
                MatchedCount = matchedCount,
                MatchedSkills = matchedSkillNames,
                MissingSkills = missingSkillNames
            });
        }

        // 5. Sort: percent DESC, matched count DESC, then newest first.
        return result
            .OrderByDescending(x => x.MatchPercent)
            .ThenByDescending(x => x.MatchedCount)
            .ThenByDescending(x => x.JobId)
            .Take(topCount)
            .ToList();
    }
}