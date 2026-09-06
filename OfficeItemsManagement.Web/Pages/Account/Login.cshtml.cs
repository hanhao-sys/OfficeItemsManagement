using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeItemsManagement.Common.Services;

namespace OfficeItemsManagement.Web.Pages.Account;

/// <summary>
/// 普通用户登录页面
/// 
/// 实验要求：
///   - 用户名密码需要符合管理员维护的用户名和密码（存储在 Users.json）
///   - 验证实现对用户名、密码不能为空的提醒
///   - 登录成功后将用户信息存入 Session
/// </summary>
public class LoginModel : PageModel
{
    private readonly JsonUserService _userService;

    public LoginModel(JsonUserService userService)
    {
        _userService = userService;
    }

    /// <summary>表单绑定的用户名</summary>
    [BindProperty]
    public string UserId { get; set; } = "";

    /// <summary>表单绑定的密码</summary>
    [BindProperty]
    public string Password { get; set; } = "";

    /// <summary>错误提示信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// GET 请求 — 显示登录页面
    /// 已登录用户直接跳转到库存查询
    /// </summary>
    public IActionResult OnGet()
    {
        // Session 中有用户 ID → 已登录 → 跳过登录页
        if (HttpContext.Session.GetString("UserId") != null)
            return RedirectToPage("/StockQuery");
        return Page();
    }

    /// <summary>
    /// POST 请求 — 处理登录表单提交
    /// 
    /// 验证流程：
    ///   1. 用户名不能为空
    ///   2. 密码不能为空
    ///   3. 调用 JsonUserService.ValidateUser 验证
    ///   4. 验证通过 → Session 存储用户信息 → 跳转库存查询
    ///   5. 验证失败 → 显示错误信息
    /// </summary>
    public IActionResult OnPost()
    {
        // ======== 验证：用户名不能为空 ========
        if (string.IsNullOrEmpty(UserId))
        {
            ErrorMessage = "用户名不能为空！";
            return Page();
        }

        // ======== 验证：密码不能为空 ========
        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "密码不能为空！";
            return Page();
        }

        // ======== 验证用户名密码 ========
        if (_userService.ValidateUser(UserId, Password))
        {
            // 登录成功 → 写入 Session
            var user = _userService.GetUserById(UserId);
            HttpContext.Session.SetString("UserId", UserId);
            HttpContext.Session.SetString("UserName", user?.Name ?? UserId);
            return RedirectToPage("/StockQuery");
        }

        // 登录失败
        ErrorMessage = "用户名或密码错误！";
        return Page();
    }
}
