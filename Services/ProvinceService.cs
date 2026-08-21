using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface IProvinceService
{
    Task<(List<Province> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize);
    Task<Province?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string provinceName, int? excludeId = null);
    Task<Province> CreateAsync(ProvinceViewModel model);
    Task<bool> UpdateAsync(int id, ProvinceViewModel model);
    Task<bool> HasRelatedRecordsAsync(int id);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id);
}

public class ProvinceService : IProvinceService
{
    private readonly StudentPartTimeJobDbContext _context;

    public ProvinceService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Province> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _context.Provinces.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.ProvinceName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.ProvinceName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Province?> GetByIdAsync(int id)
    {
        return await _context.Provinces.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string provinceName, int? excludeId = null)
    {
        var name = provinceName.Trim().ToLower();
        return await _context.Provinces
            .AnyAsync(p => p.ProvinceName.ToLower() == name && (!excludeId.HasValue || p.ProvinceId != excludeId.Value));
    }

    public async Task<Province> CreateAsync(ProvinceViewModel model)
    {
        var province = new Province
        {
            ProvinceName = model.ProvinceName.Trim(),
            CreatedAt = DateTime.Now
        };

        _context.Provinces.Add(province);
        await _context.SaveChangesAsync();
        return province;
    }

    public async Task<bool> UpdateAsync(int id, ProvinceViewModel model)
    {
        var province = await _context.Provinces.FindAsync(id);
        if (province == null) return false;

        province.ProvinceName = model.ProvinceName.Trim();
        province.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Province has no IsActive flag (per DB schema), so it is hard-delete only.
    /// Both Jobs and Companies reference ProvinceId, so both must be checked
    /// before deletion to avoid an FK violation / EF ClientSetNull exception.
    /// </summary>
    public async Task<bool> HasRelatedRecordsAsync(int id)
    {
        var hasJobs = await _context.Jobs.AnyAsync(j => j.ProvinceId == id);
        if (hasJobs) return true;

        var hasCompanies = await _context.Companies.AnyAsync(c => c.ProvinceId == id);
        return hasCompanies;
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id)
    {
        var province = await _context.Provinces.FindAsync(id);
        if (province == null)
            return (false, "Không tìm thấy tỉnh/thành.");

        if (await HasRelatedRecordsAsync(id))
            return (false, "Không thể xóa vì vẫn còn tin tuyển dụng hoặc doanh nghiệp liên kết với tỉnh/thành này.");

        _context.Provinces.Remove(province);
        await _context.SaveChangesAsync();
        return (true, null);
    }
}
