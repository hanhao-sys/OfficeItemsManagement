using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.Models;

namespace OfficeItemsManagement.Data.Repositories;

/// <summary>
/// 物品类别字典仓储 — 提供类别下拉列表数据源
/// 预置 6 个类别：纸张、文具、刀具、单据、礼品、其它
/// </summary>
public class CategoryRepository
{
    private readonly AppDbContext _db;

    public CategoryRepository(AppDbContext db) => _db = db;

    /// <summary>获取所有类别（只读）</summary>
    public async Task<List<Category>> GetAllAsync() =>
        await _db.Categories.AsNoTracking().ToListAsync();
}

/// <summary>
/// 产地字典仓储 — 提供产地下拉列表数据源
/// 预置 13 个产地：含国内主要城市和德国、日本、美国、中国台湾
/// </summary>
public class OriginRepository
{
    private readonly AppDbContext _db;

    public OriginRepository(AppDbContext db) => _db = db;

    /// <summary>获取所有产地（只读）</summary>
    public async Task<List<Origin>> GetAllAsync() =>
        await _db.Origins.AsNoTracking().ToListAsync();
}
