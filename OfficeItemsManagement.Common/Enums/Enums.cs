namespace OfficeItemsManagement.Common.Enums;

/// <summary>
/// 物品类别枚举 — 与数据库 Categories 表对应
/// 数值从 1 开始，与 Id 对应
/// </summary>
public enum ItemCategory
{
    纸张 = 1,
    文具 = 2,
    刀具 = 3,
    单据 = 4,
    礼品 = 5,
    其它 = 6
}

/// <summary>
/// 领用申请状态枚举
/// 申请(0) → 管理员审批 → 确认(1) 或 驳回(2)
/// </summary>
public enum StockOutStatus
{
    /// <summary>待审批（普通用户刚提交）</summary>
    申请 = 0,
    /// <summary>已确认，库存已扣减</summary>
    确认 = 1,
    /// <summary>已驳回</summary>
    驳回 = 2
}
