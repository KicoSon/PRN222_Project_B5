using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface IAuditService
{
    Task LogActionAsync(int? userId, string action, string entityName, int? entityId, string? description);

    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int? userId, string? action, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
}

public class AuditService : IAuditService
{
    private readonly StudentPartTimeJobDbContext _context;

    public AuditService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(int? userId, string action, string entityName, int? entityId, string? description)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            CreatedAt = DateTime.Now // using local time or SQL Server systemdatetime is typical
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Read-side query for Admin > Audit Logs. All filters optional.
    /// UserId and EntityId on AuditLog are nullable (system-generated actions),
    /// so filtering by UserId only narrows rows that actually have one.
    /// </summary>
    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int? userId, string? action, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.ToLower().Contains(action.Trim().ToLower()));

        if (dateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            query = query.Where(a => a.CreatedAt < dateTo.Value.Date.AddDays(1));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
