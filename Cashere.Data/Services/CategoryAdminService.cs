using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;
using Cashere.Services;
using Microsoft.EntityFrameworkCore;

namespace Cashere.Data.Services;

public class CategoryAdminService : ICategoryAdminService
{
    private readonly IDbContextFactory<CashereDbContext> _dbContextFactory;

    public CategoryAdminService(IDbContextFactory<CashereDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Categories.OrderBy(c => c.Name).AsNoTracking().ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(string name)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var trimmed = name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == trimmed))
        {
            throw new AdminValidationException($"A category named '{trimmed}' already exists.");
        }

        var category = new Category { Name = trimmed };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(int categoryId, string name)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId)
            ?? throw new AdminValidationException($"Category {categoryId} was not found.");

        var trimmed = name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == trimmed && c.Id != categoryId))
        {
            throw new AdminValidationException($"A category named '{trimmed}' already exists.");
        }

        category.Name = trimmed;
        await db.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null) return;

        // Products referencing this category have OnDelete(SetNull) configured,
        // so they simply lose their category rather than blocking the delete.
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }
}