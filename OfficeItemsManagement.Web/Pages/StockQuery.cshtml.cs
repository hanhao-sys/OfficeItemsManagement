using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficeItemsManagement.Common.DTOs;
using OfficeItemsManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace OfficeItemsManagement.Web.Pages;

/// <summary>
/// 库存查询页面 — 普通用户查看所有物品库存
/// 
/// 数据来源：Items JOIN Categories，返回扁平化 StockQueryDto
/// 零库存物品在 cshtml 中用红色字体标出
/// </summary>
public class StockQueryModel : PageModel
{
    private readonly AppDbContext _db;

    public StockQueryModel(AppDbContext db) => _db = db;

    /// <summary>库存数据列表</summary>
    public List<StockQueryDto> Items { get; set; } = new();

    /// <summary>
    /// GET 请求 — 加载库存数据
    /// 未登录用户重定向到登录页
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // ======== 登录检查 ========
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToPage("/Account/Login");

        // ======== JOIN 查询：Items + Categories ========
        Items = await (from i in _db.Items
                       join c in _db.Categories on i.Category equals c.Id
                       select new StockQueryDto
                       {
                           ItemCode = i.ItemCode,
                           ItemName = i.ItemName,
                           CategoryName = c.Name,      // 类别 ID → 名称
                           Origin = i.Origin,
                           Specification = i.Specification,
                           Model = i.Model,
                           Quantity = i.Quantity
                       }).ToListAsync();

        return Page();
    }
}
