namespace OfficeItemsManagement.Common.DTOs;

/// <summary>
/// 入库明细分页查询 DTO — JOIN StockIn + Items
/// </summary>
public class StockInDetailDto
{
    /// <summary>入库流水号</summary>
    public string StockInNo { get; set; } = string.Empty;

    /// <summary>物品编码</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>物品名称（JOIN 获取）</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>购买日期</summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>入库数量</summary>
    public int Quantity { get; set; }

    /// <summary>单价</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>总价</summary>
    public decimal TotalPrice { get; set; }
}
