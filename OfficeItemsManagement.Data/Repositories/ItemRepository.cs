using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.DTOs;
using OfficeItemsManagement.Common.Models;

namespace OfficeItemsManagement.Data.Repositories;

/// <summary>
/// 物品信息仓储 — 封装 Items 表的 CRUD 和查询操作
/// 使用 EF Core 的 LINQ 查询，通过 DbContext 与 MySQL 交互
/// </summary>
public class ItemRepository
{
    private readonly AppDbContext _db;

    public ItemRepository(AppDbContext db) => _db = db;

    /// <summary>获取所有物品（只读，不跟踪变更）</summary>
    public async Task<List<Item>> GetAllAsync() =>
        await _db.Items.AsNoTracking().ToListAsync();

    /// <summary>
    /// 按物品编码查找单个物品（不跟踪，避免与后续 Update 冲突）
    /// </summary>
    /// <param name="code">物品编码（主键）</param>
    /// <returns>找到返回 Item，否则 null</returns>
    public async Task<Item?> GetByCodeAsync(string code) =>
        await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ItemCode == code);

    /// <summary>新增物品</summary>
    public async Task AddAsync(Item item)
    {
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// 更新物品信息 — 先解除同主键的跟踪实体，再附加新实体
    /// 解决单例 DbContext 下同一 key 被重复跟踪的问题
    /// </summary>
    public async Task UpdateAsync(Item item)
    {
        // 查找已跟踪的同 key 实体，存在则解除跟踪
        var tracked = _db.ChangeTracker.Entries<Item>()
            .FirstOrDefault(e => e.Entity.ItemCode == item.ItemCode);
        if (tracked != null)
            tracked.State = EntityState.Detached;

        _db.Items.Update(item);
        await _db.SaveChangesAsync();
    }

    /// <summary>删除物品（先查后删）</summary>
    public async Task DeleteAsync(string code)
    {
        var item = await _db.Items.FindAsync(code);
        if (item != null)
        {
            _db.Items.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 库存查询 — JOIN Items + Categories，返回扁平化 DTO
    /// 支持按类别 ID 筛选
    /// </summary>
    /// <param name="categoryFilter">类别 ID 字符串，null 表示全部</param>
    public async Task<List<StockQueryDto>> GetStockQueryAsync(string? categoryFilter = null)
    {
        // Items JOIN Categories 获取类别名称
        var query = from i in _db.Items
                    join c in _db.Categories on i.Category equals c.Id
                    select new StockQueryDto
                    {
                        ItemCode = i.ItemCode,
                        ItemName = i.ItemName,
                        CategoryName = c.Name,
                        Origin = i.Origin,
                        Specification = i.Specification,
                        Model = i.Model,
                        Quantity = i.Quantity
                    };

        // 可选：按类别筛选
        if (!string.IsNullOrEmpty(categoryFilter) && int.TryParse(categoryFilter, out var catId))
            query = query.Where(q => _db.Items
                .FirstOrDefault(x => x.ItemCode == q.ItemCode)!.Category == catId);

        return await query.ToListAsync();
    }

    /// <summary>
    /// 模糊搜索物品 — 按编码或名称匹配
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    public async Task<List<Item>> SearchAsync(string keyword) =>
        await _db.Items.AsNoTracking()
            .Where(i => i.ItemName.Contains(keyword) || i.ItemCode.Contains(keyword))
            .ToListAsync();
}
