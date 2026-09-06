namespace OfficeItemsManagement.Common.DTOs;

/// <summary>
/// 领用申请请求 DTO — Web 端提交申请时使用
/// </summary>
public class ApplyRequestDto
{
    /// <summary>要领用的物品编码</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>领用数量，必须 > 0</summary>
    public int Quantity { get; set; }

    /// <summary>领用人 ID（当前登录用户）</summary>
    public string ApplicantId { get; set; } = string.Empty;
}
