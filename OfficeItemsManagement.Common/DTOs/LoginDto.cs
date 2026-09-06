namespace OfficeItemsManagement.Common.DTOs;

/// <summary>
/// 登录请求 DTO — 管理端和 Web 端共用
/// </summary>
public class LoginDto
{
    /// <summary>用户名/账号</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>密码</summary>
    public string Password { get; set; } = string.Empty;
}
