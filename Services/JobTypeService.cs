using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface IJobTypeService
{
    Task<(List<JobType> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize);
    Task<JobType?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string typeName, int? excludeId = null);
    Task<JobType> CreateAsync(JobTypeViewModel model);
    Task<bool> UpdateAsync(int id, JobTypeViewModel model);
    Task<bool> ToggleStatusAsync(int id);
}

public class JobTypeService : IJobTypeService
{
    private readonly StudentPartTimeJobDbContext _context;

    public JobTypeService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task<(List<JobType> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _context.JobTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.TypeName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(t => t.TypeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<JobType?> GetByIdAsync(int id)
    {
        return await _context.JobTypes.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string typeName, int? excludeId = null)
    {
        var name = typeName.Trim().ToLower();
        return await _context.JobTypes
            .AnyAsync(t => t.TypeName.ToLower() == name && (!excludeId.HasValue || t.JobTypeId != excludeId.Value));
    }

    public async Task<JobType> CreateAsync(JobTypeViewModel model)
    {
        var jobType = new JobType
        {
            TypeName = model.TypeName.Trim(),
            Description = model.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.JobTypes.Add(jobType);
        await _context.SaveChangesAsync();
        return jobType;
    }

    public async Task<bool> UpdateAsync(int id, JobTypeViewModel model)
    {
        var jobType = await _context.JobTypes.FindAsync(id);
        if (jobType == null) return false;

        jobType.TypeName = model.TypeName.Trim();
        jobType.Description = model.Description?.Trim();
        jobType.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var jobType = await _context.JobTypes.FindAsync(id);
        if (jobType == null) return false;

        jobType.IsActive = !jobType.IsActive;
        jobType.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }
}
