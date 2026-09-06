using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeItemsManagement.Common.Models;
using OfficeItemsManagement.Data;
using OfficeItemsManagement.Data.Repositories;
using OfficeItemsManagement.Common.DTOs;

namespace OfficeItemsManagement.Web.Pages;

/// <summary>
/// 领用申请页面 — 普通用户提交物品领用申请
/// 
/// 实验要求：
///   - 领用流水号自动生成（SO + yyyyMMdd + 序号）
///   - 物品选择：下拉框 + 弹出窗口两种方式
///   - 领用人自动获取（当前登录用户）
///   - 领用日期自动生成
///   - 状态自动设为"申请"
///   - 提交前校验库存是否充足
/// 
/// 页面布局：
///   上半部分：领用申请表单
///   下半部分：我的领用记录（状态跟踪）
/// </summary>
public class ApplyModel : PageModel
{
    private readonly ItemRepository _itemRepo;
    private readonly StockOutRepository _stockOutRepo;
    private readonly AppDbContext _db;

    public ApplyModel(AppDbContext db, ItemRepository itemRepo, StockOutRepository stockOutRepo)
    {
        _db = db;
        _itemRepo = itemRepo;
        _stockOutRepo = stockOutRepo;
    }

    /// <summary>所有可选物品列表（下拉框和弹窗数据源）</summary>
    public List<Item> Items { get; set; } = new();

    /// <summary>当前用户的领用记录</summary>
    public List<StockOutDetailDto> MyRecords { get; set; } = new();

    /// <summary>表单：选中的物品编码</summary>
    [BindProperty]
    public string ItemCode { get; set; } = "";

    /// <summary>表单：领用数量</summary>
    [BindProperty]
    public int Quantity { get; set; }

    /// <summary>当前登录用户名（显示用）</summary>
    public string? ApplicantName { get; set; }

    /// <summary>操作结果消息</summary>
    public string? Message { get; set; }

    /// <summary>操作是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// GET 请求 — 加载物品列表和我的领用记录
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        ApplicantName = HttpContext.Session.GetString("UserName") ?? userId;
        Items = await _itemRepo.GetAllAsync();
        MyRecords = await _stockOutRepo.GetByUserAsync(userId);
        return Page();
    }

    /// <summary>
    /// POST 请求 — 提交领用申请
    /// 
    /// 校验流程：
    ///   1. 是否选择了物品
    ///   2. 数量是否 > 0
    ///   3. 物品是否存在
    ///   4. 库存是否充足
    ///   5. 调用 SubmitApplyAsync 提交
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null)
            return RedirectToPage("/Account/Login");

        ApplicantName = HttpContext.Session.GetString("UserName") ?? userId;
        Items = await _itemRepo.GetAllAsync();
        MyRecords = await _stockOutRepo.GetByUserAsync(userId);

        // ======== 表单验证 ========
        if (string.IsNullOrEmpty(ItemCode))
        {
            Message = "请选择领用物品！";
            IsSuccess = false;
            return Page();
        }

        if (Quantity <= 0)
        {
            Message = "领用数量必须大于 0！";
            IsSuccess = false;
            return Page();
        }

        // ======== 查找物品 ========
        var item = await _itemRepo.GetByCodeAsync(ItemCode);
        if (item == null)
        {
            Message = "物品不存在！";
            IsSuccess = false;
            return Page();
        }

        // ======== 库存校验 ========
        if (item.Quantity < Quantity)
        {
            Message = $"库存不足！当前库存 {item.Quantity}，申请数量 {Quantity}。";
            IsSuccess = false;
            return Page();
        }

        // ======== 提交申请 ========
        try
        {
            await _stockOutRepo.SubmitApplyAsync(ItemCode, Quantity, userId);
            Message = "领用申请已提交，等待管理员审批！";
            IsSuccess = true;

            // 刷新"我的记录"列表
            MyRecords = await _stockOutRepo.GetByUserAsync(userId);
        }
        catch (Exception ex)
        {
            Message = $"提交失败: {ex.Message}";
            IsSuccess = false;
        }

        return Page();
    }
}
