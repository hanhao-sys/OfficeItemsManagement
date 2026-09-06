namespace OfficeItemsManagement.Common.DTOs;

/// <summary>
/// 普通用户数据传输对象 — 存储在 JSON 文件中
/// 对应实验要求：用户信息存在文件中（JSON）
/// </summary>
public class AppUserDto
{
    /// <summary>用户 ID（登录账号），唯一标识</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>登录密码</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>真实姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>性别："男" 或 "女"</summary>
    public string Gender { get; set; } = "男";

    /// <summary>出生日期</summary>
    public DateTime BirthDate { get; set; }

    /// <summary>联系电话</summary>
    public string Phone { get; set; } = string.Empty;
}
