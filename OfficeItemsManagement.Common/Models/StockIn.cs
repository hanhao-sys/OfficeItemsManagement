using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeItemsManagement.Common.Models;

/// <summary>
/// 入库记录实体 — 对应数据库 StockIn 表
/// 每次物品采购入库生成一条记录
/// </summary>
[Table("StockIn")]
public class StockIn
{
    /// <summary>
    /// 入库流水号（主键），自动生成
    /// 格式：SI + 日期(yyyyMMdd) + 3位序号，如 SI20250101001
    /// </summary>
    [Key]
    [MaxLength(20)]
    public string StockInNo { get; set; } = string.Empty;

    /// <summary>入库物品编码 — 外键关联 Items 表</summary>
    [Required]
    [MaxLength(20)]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>采购/购买日期</summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>本次购买数量，必须 > 0</summary>
    public int Quantity { get; set; }

    /// <summary>物品单价（元），精确到分</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>总价 = 数量 × 单价，自动计算</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>导航属性 — EF Core 用于 JOIN 查询</summary>
    [ForeignKey(nameof(ItemCode))]
    public Item? Item { get; set; }
}
