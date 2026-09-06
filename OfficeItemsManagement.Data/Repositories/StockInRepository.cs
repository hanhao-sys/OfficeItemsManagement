using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.Models;

namespace OfficeItemsManagement.Data.Repositories;

/// <summary>
/// 入库记录仓储 — 管理 StockIn 表的读写
/// 核心方法 ExecuteStockInAsync 使用数据库事务保证原子性：
///   1. 生成流水号
///   2. 写入入库记录
///   3. 更新物品库存（累加）
/// 任一步失败则回滚
/// </summary>
public class StockInRepository
{
    private readonly AppDbContext _db;

    public StockInRepository(AppDbContext db) => _db = db;

    /// <summary>获取所有入库记录（含物品导航属性），按日期倒序</summary>
    public async Task<List<StockIn>> GetAllAsync() =>
        await _db.StockIns.AsNoTracking()
            .Include(s => s.Item)           // 预加载物品信息
            .OrderByDescending(s => s.PurchaseDate)
            .ToListAsync();

    /// <summary>
    /// 获取最近 N 条入库记录 — 用于入库页面 DataGrid 展示
    /// 避免百万数据全量加载导致卡死
    /// </summary>
    /// <param name="count">取最近多少条，默认 50</param>
    public async Task<List<StockIn>> GetRecentAsync(int count = 50) =>
        await _db.StockIns.AsNoTracking()
            .Include(s => s.Item)
            .OrderByDescending(s => s.PurchaseDate)
            .Take(count)
            .ToListAsync();

    /// <summary>直接新增入库记录（不更新库存）</summary>
    public async Task AddAsync(StockIn stockIn)
    {
        _db.StockIns.Add(stockIn);
        await _db.SaveChangesAsync();
    }

    /// <summary>删除入库记录（触发器会自动回退库存）</summary>
    public async Task DeleteAsync(string stockInNo)
    {
        var entity = await _db.StockIns.FindAsync(stockInNo);
        if (entity != null)
        {
            _db.StockIns.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 执行入库操作（带事务）
    /// 
    /// 流水号生成规则：
    ///   SI + 当前日期(yyyyMMdd) + 当日序号(3位补零)
    ///   例：SI20250115001 表示 2025-01-15 的第一笔入库
    ///
    /// 事务流程：
    ///   1. 查询当天已有入库数，生成新流水号
    ///   2. INSERT StockIn 记录
    ///   3. UPDATE Items SET Quantity = Quantity + 入库数量
    ///   4. 提交事务
    ///   任何步骤失败 → 回滚，数据保持一致
    /// </summary>
    /// <param name="item">入库的物品实体（已从 DB 加载）</param>
    /// <param name="quantity">入库数量</param>
    /// <param name="unitPrice">单价</param>
    /// <returns>创建成功的 StockIn 记录</returns>
    public async Task<StockIn> ExecuteStockInAsync(Item item, int quantity, decimal unitPrice)
    {
        // 开启数据库事务
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 步骤 1：生成流水号 SI + yyyyMMdd + 序号
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var todayCount = await _db.StockIns
                .CountAsync(s => s.StockInNo.StartsWith("SI" + datePart));
            var stockInNo = $"SI{datePart}{todayCount + 1:D3}";

            // 步骤 2：构建入库记录
            var stockIn = new StockIn
            {
                StockInNo = stockInNo,
                ItemCode = item.ItemCode,
                PurchaseDate = DateTime.Now,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = quantity * unitPrice  // 总价 = 数量 × 单价
            };

            // 步骤 3：写入库记录
            _db.StockIns.Add(stockIn);

            // 步骤 4：更新物品库存（累加）
            item.Quantity += quantity;
            _db.Items.Update(item);

            await _db.SaveChangesAsync();

            // 步骤 5：提交事务
            await tx.CommitAsync();

            return stockIn;
        }
        catch
        {
            // 任何异常 → 回滚所有操作
            await tx.RollbackAsync();
            throw;
        }
    }
}
