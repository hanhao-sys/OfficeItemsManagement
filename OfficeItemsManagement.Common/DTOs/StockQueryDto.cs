namespace OfficeItemsManagement.Common.DTOs;

/// <summary>
/// 库存查询结果 DTO — 多表 JOIN 后的扁平化视图
/// 用于管理端和 Web 端的库存总览
/// </summary>
public class StockQueryDto
{
    /// <summary>物品编码</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>物品名称</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>类别名称（JOIN Categories 获取）</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>产地</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>规格</summary>
    public string Specification { get; set; } = string.Empty;

    /// <summary>型号</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>当前库存数量，0 时前端标红</summary>
    public int Quantity { get; set; }
}
