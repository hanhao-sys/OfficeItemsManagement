using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeItemsManagement.Common.Models;

/// <summary>
/// 领用/出库记录实体 — 对应数据库 StockOut 表
/// 普通用户提交领用申请 → 管理员审批（确认/驳回）
/// </summary>
[Table("StockOut")]
public class StockOut
{
    /// <summary>
    /// 领用流水号（主键），自动生成
    /// 格式：SO + 日期(yyyyMMdd) + 3位序号，如 SO20250103001
    /// </summary>
    [Key]
    [MaxLength(20)]
    public string StockOutNo { get; set; } = string.Empty;

    /// <summary>领用物品编码 — 外键关联 Items 表</summary>
    [Required]
    [MaxLength(20)]
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>领用数量，必须 > 0</summary>
    public int Quantity { get; set; }

    /// <summary>领用人 ID — 对应 JSON 文件中的 UserId</summary>
    [Required]
    [MaxLength(50)]
    public string ApplicantId { get; set; } = string.Empty;

    /// <summary>领用申请日期</summary>
    public DateTime ApplyDate { get; set; }

    /// <summary>
    /// 审批状态：
    /// 0 = 申请（待审批）
    /// 1 = 确认（已通过，库存已扣减）
    /// 2 = 驳回（已拒绝）
    /// </summary>
    public int Status { get; set; }

    /// <summary>审批日期，确认或驳回时填写</summary>
    public DateTime? ApproveDate { get; set; }

    /// <summary>审批备注，可填写驳回原因等</summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>导航属性 — EF Core 用于 JOIN 查询</summary>
    [ForeignKey(nameof(ItemCode))]
    public Item? Item { get; set; }
}
