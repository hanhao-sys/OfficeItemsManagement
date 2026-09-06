namespace OfficeItemsManagement.Common.DTOs;

/// <summary>
/// 领用明细分页查询 DTO — JOIN StockOut + Items
/// 用于管理端审批列表和 Web 端"我的记录"
/// </summary>
public class StockOutDetailDto
{
    /// <summary>领用流水号</summary>
    public string StockOutNo { get; set; } = string.Empty;

    /// <summary>物品编码</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>物品名称（JOIN 获取）</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>领用数量</summary>
    public int Quantity { get; set; }

    /// <summary>领用人 ID</summary>
    public string ApplicantId { get; set; } = string.Empty;

    /// <summary>领用人姓名（前端显示用）</summary>
    public string ApplicantName { get; set; } = string.Empty;

    /// <summary>领用申请日期</summary>
    public DateTime ApplyDate { get; set; }

    /// <summary>状态文字："申请" / "确认" / "驳回"</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>状态数值：0=申请 1=确认 2=驳回</summary>
    public int Status { get; set; }
}
