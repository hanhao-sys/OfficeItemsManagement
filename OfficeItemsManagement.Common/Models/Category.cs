using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeItemsManagement.Common.Models;

/// <summary>
/// 物品类别字典实体 — 对应 Categories 表
/// 预置：纸张、文具、刀具、单据、礼品、其它
/// </summary>
[Table("Categories")]
public class Category
{
    /// <summary>类别 ID（自增主键）</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>类别名称，唯一</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
}
