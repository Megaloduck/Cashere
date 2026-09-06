using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cashere.Models;

namespace Cashere.Services;

public interface ICategoryAdminService
{
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Category> CreateCategoryAsync(string name);
    Task UpdateCategoryAsync(int categoryId, string name);
    Task DeleteCategoryAsync(int categoryId);
}