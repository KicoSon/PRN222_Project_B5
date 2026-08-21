using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface ICompanyService
{
    Task<(List<Company> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int page, int pageSize);
    Task<Company?> GetByIdAsync(int id);
    Task<(bool Success, string? ErrorMessage)> ApproveAsync(int id);
    Task<(bool Success, string? ErrorMessage)> RejectAsync(int id, string reason);
    Task<(bool Success, string? ErrorMessage)> BlockAsync(int id);
    Task<(bool Success, string? ErrorMessage)> ActivateAsync(int id);
}

public class CompanyService : ICompanyService
{
    private readonly StudentPartTimeJobDbContext _context;

    public CompanyService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Company> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int page, int pageSize)
    {
        var query = _context.Companies
            .Include(c => c.Province)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.CompanyName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Company?> GetByIdAsync(int id)
    {
        return await _context.Companies
            .Include(c => c.Province)
            .Include(c => c.Employers)
            .FirstOrDefaultAsync(c => c.CompanyId == id);
    }

    // ---- Pair 1: initial review (mirrors Job Approve/Reject) ----

    public async Task<(bool Success, string? ErrorMessage)> ApproveAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return (false, "Không tìm thấy công ty.");

        if (company.Status != "Pending")
            return (false, "Chỉ có thể duyệt công ty đang ở trạng thái Chờ duyệt.");

        company.Status = "Approved";
        company.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> RejectAsync(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return (false, "Lý do từ chối là bắt buộc.");

        var company = await _context.Companies.FindAsync(id);
        if (company == null) return (false, "Không tìm thấy công ty.");

        if (company.Status != "Pending")
            return (false, "Chỉ có thể từ chối công ty đang ở trạng thái Chờ duyệt.");

        company.Status = "Rejected";
        company.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    // ---- Pair 2: ongoing moderation (mirrors User Lock/Unlock) ----

    public async Task<(bool Success, string? ErrorMessage)> BlockAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return (false, "Không tìm thấy công ty.");

        if (company.Status != "Approved")
            return (false, "Chỉ có thể chặn công ty đang ở trạng thái Đã duyệt.");

        company.Status = "Blocked";
        company.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> ActivateAsync(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return (false, "Không tìm thấy công ty.");

        if (company.Status != "Blocked")
            return (false, "Chỉ có thể kích hoạt lại công ty đang bị chặn.");

        company.Status = "Approved";
        company.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return (true, null);
    }
}
