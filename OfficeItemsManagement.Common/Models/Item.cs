using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeItemsManagement.Common.Models;

/// <summary>
/// 物品信息实体 — 对应数据库 Items 表
/// 存储所有办公物品的基本信息和库存数量
/// 图片数据存储在 ImageData 字段（LONGBLOB），同时保留 ImagePath 作历史兼容
/// </summary>
[Table("Items")]
public class Item
{
    /// <summary>
    /// 物品编码（主键），唯一标识，如 P001、S002
    /// 编码规则：首字母表示类别 + 数字序号
    /// </summary>
    [Key]
    [MaxLength(20)]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>物品名称，必填，最长 100 字符</summary>
    [Required]
    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// 物品类别 ID — 外键关联 Categories 表
    /// 1=纸张 2=文具 3=刀具 4=单据 5=礼品 6=其它
    /// </summary>
    public int Category { get; set; }

    /// <summary>产地，如"北京""德国"等，最长 100 字符</summary>
    [MaxLength(100)]
    public string Origin { get; set; } = string.Empty;

    /// <summary>规格描述，如"500张/包"，最长 100 字符</summary>
    [MaxLength(100)]
    public string Specification { get; set; } = string.Empty;

    /// <summary>型号，如"GP-1008"，最长 100 字符</summary>
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    /// <summary>物品图片的本地文件路径（历史兼容，新增图片优先存 DB）</summary>
    [MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// 图片二进制数据 — 存储在数据库 LONGBLOB 列
    /// 最大约 4GB，实际限制于 MySQL max_allowed_packet（默认 64MB）
    /// </summary>
    [Column(TypeName = "LONGBLOB")]
    public byte[]? ImageData { get; set; }

    /// <summary>
    /// 当前库存数量 — 入库时累加，领用确认时扣减
    /// 数据库有 CHECK 约束保证 >= 0
    /// </summary>
    public int Quantity { get; set; }
}
