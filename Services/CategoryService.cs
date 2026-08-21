using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface ICategoryService
{
    Task<(List<Category> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize);
    Task<Category?> GetByIdAsync(int id);
    Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null);
    Task<Category> CreateAsync(CategoryViewModel model);
    Task<bool> UpdateAsync(int id, CategoryViewModel model);
    Task<bool> ToggleStatusAsync(int id);
}

public class CategoryService : ICategoryService
{
    private readonly StudentPartTimeJobDbContext _context;

    public CategoryService(StudentPartTimeJobDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Category> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.CategoryName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.CategoryName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<bool> ExistsByNameAsync(string categoryName, int? excludeId = null)
    {
        var name = categoryName.Trim().ToLower();
        return await _context.Categories
            .AnyAsync(c => c.CategoryName.ToLower() == name && (!excludeId.HasValue || c.CategoryId != excludeId.Value));
    }

    public async Task<Category> CreateAsync(CategoryViewModel model)
    {
        var category = new Category
        {
            CategoryName = model.CategoryName.Trim(),
            Description = model.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> UpdateAsync(int id, CategoryViewModel model)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        category.CategoryName = model.CategoryName.Trim();
        category.Description = model.Description?.Trim();
        category.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }
}
