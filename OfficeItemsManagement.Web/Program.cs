using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.Services;
using OfficeItemsManagement.Data;
using OfficeItemsManagement.Data.Repositories;

namespace OfficeItemsManagement.Web;

/// <summary>
/// ASP.NET Core Web 应用程序入口
/// 
/// 服务注册：
///   - DbContext：EF Core + Pomelo MySQL 提供程序
///   - Repository：物品/领用/类别/产地仓储
///   - JsonUserService：JSON 文件用户存储（单例）
///   - Session：用于保存登录状态
/// 
/// 中间件管线：
///   StaticFiles → Routing → Session → RazorPages
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ======== 注册数据库上下文 ========
        // Pomelo.EntityFrameworkCore.MySql 连接 MySQL 8.4.8
        var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

        // ======== 注册仓储（Scoped：每个请求一个实例） ========
        builder.Services.AddScoped<ItemRepository>();
        builder.Services.AddScoped<StockOutRepository>();
        builder.Services.AddScoped<CategoryRepository>();
        builder.Services.AddScoped<OriginRepository>();

        // ======== 注册 JSON 用户服务（Singleton：全站共享） ========
        builder.Services.AddSingleton(sp =>
        {
            var path = Path.Combine(builder.Environment.ContentRootPath, "..", "Users.json");
            return new JsonUserService(path);
        });

        // ======== 注册 Session（用于保存登录用户信息） ========
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);  // 30 分钟无操作自动过期
            options.Cookie.HttpOnly = true;                   // 防止 JS 读取 Cookie
            options.Cookie.IsEssential = true;                // GDPR 豁免
        });

        builder.Services.AddRazorPages();

        var app = builder.Build();

        // ======== 中间件管线 ========
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();    // 提供 wwwroot 下的静态文件（CSS/JS）
        app.UseRouting();        // 路由匹配
        app.UseSession();        // Session 中间件
        app.MapRazorPages();     // Razor Pages 终结点

        app.Run();
    }
}
