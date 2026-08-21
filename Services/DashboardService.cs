using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface IDashboardService
{
    // Job Statistics
    Task<JobStatisticsViewModel> GetJobStatisticsAsync();

    // Application Statistics
    Task<ApplicationStatisticsViewModel> GetApplicationStatisticsAsync(int months = 12);

    // Company Statistics
    Task<CompanyStatisticsViewModel> GetCompanyStatisticsAsync(int topCount = 10);

    // Student Statistics
    Task<StudentStatisticsViewModel> GetStudentStatisticsAsync(int topCategoriesCount = 10);
}

public class DashboardService : IDashboardService
{
    private readonly StudentPartTimeJobDbContext _context;

    public DashboardService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task<JobStatisticsViewModel> GetJobStatisticsAsync()
    {
        var byCategory = await _context.Jobs
            .GroupBy(j => j.Category.CategoryName)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var byProvince = await _context.Jobs
            .GroupBy(j => j.Province.ProvinceName)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var byJobType = await _context.Jobs
            .GroupBy(j => j.JobType.TypeName)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var byStatus = await _context.Jobs
            .GroupBy(j => j.Status)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        return new JobStatisticsViewModel
        {
            ByCategory = byCategory,
            ByProvince = byProvince,
            ByJobType = byJobType,
            ByStatus = byStatus
        };
    }

    public async Task<ApplicationStatisticsViewModel> GetApplicationStatisticsAsync(int months = 12)
    {
        var totalApplications = await _context.Applications.CountAsync();

        var byStatus = await _context.Applications
            .GroupBy(a => a.Status)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        // Pull raw year/month/count from the DB, then format labels in memory
        // (EF Core cannot translate CultureInfo-based month formatting to SQL).
        var cutoff = DateTime.Today.AddMonths(-(months - 1));
        var rawMonthly = await _context.Applications
            .Where(a => a.AppliedAt >= new DateTime(cutoff.Year, cutoff.Month, 1))
            .GroupBy(a => new { a.AppliedAt.Year, a.AppliedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        var byMonth = new List<MonthlyCountViewModel>();
        for (int i = months - 1; i >= 0; i--)
        {
            var monthDate = DateTime.Today.AddMonths(-i);
            var match = rawMonthly.FirstOrDefault(x => x.Year == monthDate.Year && x.Month == monthDate.Month);
            byMonth.Add(new MonthlyCountViewModel
            {
                MonthLabel = $"T{monthDate.Month}/{monthDate.Year}",
                Count = match?.Count ?? 0
            });
        }

        return new ApplicationStatisticsViewModel
        {
            TotalApplications = totalApplications,
            ByStatus = byStatus,
            ByMonth = byMonth
        };
    }

    public async Task<CompanyStatisticsViewModel> GetCompanyStatisticsAsync(int topCount = 10)
    {
        var totalCompanies = await _context.Companies.CountAsync();

        var byStatus = await _context.Companies
            .GroupBy(c => c.Status)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var topCompanies = await _context.Companies
            .Select(c => new CompanyJobCountViewModel
            {
                CompanyName = c.CompanyName,
                JobCount = c.Employers.SelectMany(e => e.Jobs).Count()
            })
            .OrderByDescending(x => x.JobCount)
            .Take(topCount)
            .ToListAsync();

        return new CompanyStatisticsViewModel
        {
            TotalCompanies = totalCompanies,
            ByStatus = byStatus,
            TopCompanies = topCompanies
        };
    }

    public async Task<StudentStatisticsViewModel> GetStudentStatisticsAsync(int topCategoriesCount = 10)
    {
        var totalStudents = await _context.Students.CountAsync();

        var byMajor = await _context.Students
            .Where(s => s.Major != null)
            .GroupBy(s => s.Major)
            .Select(g => new ChartDataPointViewModel { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var byUniversity = await _context.Students
            .Where(s => s.University != null)
            .GroupBy(s => s.University)
            .Select(g => new ChartDataPointViewModel { Label = g.Key!, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        // Aggregate, system-wide: which job categories draw the most applications overall.
        var topAppliedCategories = await _context.Applications
            .GroupBy(a => a.Job.Category.CategoryName)
            .Select(g => new ChartDataPointViewModel { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .Take(topCategoriesCount)
            .ToListAsync();

        return new StudentStatisticsViewModel
        {
            TotalStudents = totalStudents,
            ByMajor = byMajor,
            ByUniversity = byUniversity,
            TopAppliedCategories = topAppliedCategories
        };
    }
}
