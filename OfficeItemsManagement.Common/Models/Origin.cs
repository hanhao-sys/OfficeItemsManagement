using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OfficeItemsManagement.Common.Models;

/// <summary>
/// 产地字典实体 — 对应 Origins 表
/// 预置：北京、上海、广州、深圳、杭州、成都、武汉、南京、苏州、德国、日本、美国、中国台湾
/// </summary>
[Table("Origins")]
public class Origin
{
    /// <summary>产地 ID（自增主键）</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>产地名称，唯一</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
