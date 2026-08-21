using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, string title, string? content, string type);

    Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task<Notification?> GetByIdAsync(int id);
    Task<bool> MarkAsReadAsync(int id);
}

public class NotificationService : INotificationService
{
    private readonly StudentPartTimeJobDbContext _context;

    public NotificationService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task CreateNotificationAsync(int userId, string title, string? content, string type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// System-wide notification feed for Admin oversight (not scoped to the
    /// current admin's own UserId — no code path currently creates
    /// notifications addressed to Admin accounts).
    /// </summary>
    public async Task<(List<Notification> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Notifications
            .Include(n => n.User)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .Include(n => n.User)
            .FirstOrDefaultAsync(n => n.NotificationId == id);
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
