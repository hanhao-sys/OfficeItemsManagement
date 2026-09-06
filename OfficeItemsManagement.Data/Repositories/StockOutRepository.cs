using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.DTOs;
using OfficeItemsManagement.Common.Models;

namespace OfficeItemsManagement.Data.Repositories;

/// <summary>
/// 领用/出库记录仓储 — 管理 StockOut 表的全部操作
/// 
/// 业务流程：
///   普通用户 Web 端提交申请（Status=0）
///   → 管理员桌面端查看待审批列表
///   → 确认（Status=1，扣库存，事务保护）
///   → 驳回（Status=2，不扣库存）
/// </summary>
public class StockOutRepository
{
    private readonly AppDbContext _db;

    public StockOutRepository(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取所有领用申请 — 管理员审批列表
    /// JOIN Items 获取物品名称，按申请日期倒序
    /// </summary>
    public async Task<List<StockOutDetailDto>> GetAllAsync()
    {
        return await (from so in _db.StockOuts
                      join i in _db.Items on so.ItemCode equals i.ItemCode
                      orderby so.ApplyDate descending
                      select new StockOutDetailDto
                      {
                          StockOutNo = so.StockOutNo,
                          ItemCode = so.ItemCode,
                          ItemName = i.ItemName,
                          Quantity = so.Quantity,
                          ApplicantId = so.ApplicantId,
                          ApplicantName = so.ApplicantId,
                          ApplyDate = so.ApplyDate,
                          Status = so.Status,
                          StatusText = so.Status == 0 ? "申请" : so.Status == 1 ? "确认" : "驳回"
                      }).ToListAsync();
    }

    /// <summary>
    /// 获取某个用户的所有领用记录 — Web 端"我的记录"
    /// </summary>
    /// <param name="userId">当前登录用户的 ID</param>
    public async Task<List<StockOutDetailDto>> GetByUserAsync(string userId)
    {
        return await (from so in _db.StockOuts
                      join i in _db.Items on so.ItemCode equals i.ItemCode
                      where so.ApplicantId == userId
                      orderby so.ApplyDate descending
                      select new StockOutDetailDto
                      {
                          StockOutNo = so.StockOutNo,
                          ItemCode = so.ItemCode,
                          ItemName = i.ItemName,
                          Quantity = so.Quantity,
                          ApplicantId = so.ApplicantId,
                          ApplicantName = so.ApplicantId,
                          ApplyDate = so.ApplyDate,
                          Status = so.Status,
                          StatusText = so.Status == 0 ? "申请" : so.Status == 1 ? "确认" : "驳回"
                      }).ToListAsync();
    }

    /// <summary>
    /// 提交领用申请 — 普通用户在 Web 端操作
    /// 流水号格式：SO + yyyyMMdd + 3位序号
    /// 状态初始为 0（申请），等待管理员审批
    /// </summary>
    /// <param name="itemCode">要领用的物品编码</param>
    /// <param name="quantity">领用数量</param>
    /// <param name="applicantId">领用人 ID</param>
    /// <returns>创建成功的 StockOut 记录</returns>
    public async Task<StockOut> SubmitApplyAsync(string itemCode, int quantity, string applicantId)
    {
        // 生成流水号
        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var todayCount = await _db.StockOuts
            .CountAsync(s => s.StockOutNo.StartsWith("SO" + datePart));
        var stockOutNo = $"SO{datePart}{todayCount + 1:D3}";

        var stockOut = new StockOut
        {
            StockOutNo = stockOutNo,
            ItemCode = itemCode,
            Quantity = quantity,
            ApplicantId = applicantId,
            ApplyDate = DateTime.Now,
            Status = 0 // 申请（待审批）
        };

        _db.StockOuts.Add(stockOut);
        await _db.SaveChangesAsync();
        return stockOut;
    }

    /// <summary>
    /// 确认领用 — 管理员审批通过
    /// 
    /// 事务保证原子性：
    ///   1. 检查申请是否存在且状态为"申请"
    ///   2. 检查库存是否充足
    ///   3. 更新 StockOut 状态为"确认"
    ///   4. 扣减 Items 库存
    ///   5. 提交
    /// 任一步失败 → 回滚
    /// </summary>
    /// <param name="stockOutNo">领用流水号</param>
    /// <param name="remark">审批备注</param>
    public async Task ApproveAsync(string stockOutNo, string remark = "")
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. 查找领用记录
            var stockOut = await _db.StockOuts.FindAsync(stockOutNo)
                ?? throw new InvalidOperationException("领用记录不存在");

            // 2. 状态校验：只能确认"申请"状态的记录
            if (stockOut.Status != 0)
                throw new InvalidOperationException("该申请已处理");

            // 3. 查找物品
            var item = await _db.Items.FindAsync(stockOut.ItemCode)
                ?? throw new InvalidOperationException("物品不存在");

            // 4. 库存校验
            if (item.Quantity < stockOut.Quantity)
                throw new InvalidOperationException("库存不足");

            // 5. 更新领用状态
            stockOut.Status = 1;               // 确认
            stockOut.ApproveDate = DateTime.Now;
            stockOut.Remark = remark;

            // 6. 扣减库存
            item.Quantity -= stockOut.Quantity;

            _db.StockOuts.Update(stockOut);
            _db.Items.Update(item);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 驳回领用申请 — 管理员拒绝
    /// 不扣减库存，仅更新状态为"驳回"
    /// </summary>
    /// <param name="stockOutNo">领用流水号</param>
    /// <param name="remark">驳回原因</param>
    public async Task RejectAsync(string stockOutNo, string remark = "")
    {
        var stockOut = await _db.StockOuts.FindAsync(stockOutNo)
            ?? throw new InvalidOperationException("领用记录不存在");

        // 状态校验：只能驳回"申请"状态的记录
        if (stockOut.Status != 0)
            throw new InvalidOperationException("该申请已处理");

        stockOut.Status = 2;               // 驳回
        stockOut.ApproveDate = DateTime.Now;
        stockOut.Remark = remark;

        _db.StockOuts.Update(stockOut);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// 领用明细分页查询 — 库存查询页的"领用明细"Tab
    /// 支持按物品编码筛选，按日期倒序，分页返回
    /// </summary>
    /// <param name="itemCode">物品编码筛选，null 表示全部</param>
    /// <param name="pageNo">页码（从 1 开始）</param>
    /// <param name="pageSize">每页条数</param>
    public async Task<List<StockOutDetailDto>> GetDetailPageAsync(
        string? itemCode, int pageNo, int pageSize)
    {
        var query = from so in _db.StockOuts
                    join i in _db.Items on so.ItemCode equals i.ItemCode
                    select new StockOutDetailDto
                    {
                        StockOutNo = so.StockOutNo,
                        ItemCode = so.ItemCode,
                        ItemName = i.ItemName,
                        Quantity = so.Quantity,
                        ApplicantId = so.ApplicantId,
                        ApplicantName = so.ApplicantId,
                        ApplyDate = so.ApplyDate,
                        Status = so.Status,
                        StatusText = so.Status == 0 ? "申请" : so.Status == 1 ? "确认" : "驳回"
                    };

        // 可选：按物品编码筛选
        if (!string.IsNullOrEmpty(itemCode))
            query = query.Where(q => q.ItemCode == itemCode);

        // 分页：跳过前 (pageNo-1)*pageSize 条，取 pageSize 条
        return await query
            .OrderByDescending(q => q.ApplyDate)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
