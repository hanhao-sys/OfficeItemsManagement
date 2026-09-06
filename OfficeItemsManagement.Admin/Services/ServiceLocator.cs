using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.Services;
using OfficeItemsManagement.Data;
using OfficeItemsManagement.Data.Repositories;
using System.IO;

namespace OfficeItemsManagement.Admin.Services;

/// <summary>
/// 服务定位器 — 简易依赖注入替代方案
/// 
/// 由于 WPF 桌面应用的生命周期与 ASP.NET Core 不同，
/// 这里使用静态服务定位器模式统一管理所有依赖：
///   - DbContext（EF Core 数据库连接）
///   - JsonUserService（JSON 文件用户存储）
///   - 各 Repository（数据仓储）
///
/// 连接字符串在此修改 → ConnectionString 属性
/// </summary>
public static class ServiceLocator
{
    private static AppDbContext? _dbContext;
    private static JsonUserService? _userService;

    /// <summary>
    /// MySQL 数据库连接字符串
    /// 修改此处的 Server/User/Password 即可切换数据库
    /// </summary>
    public static string ConnectionString { get; set; } =
        "Server=localhost;Port=3306;Database=OfficeItemsDB;User=root;Password=031020;Charset=utf8mb4;";

    /// <summary>
    /// EF Core DbContext 单例
    /// 首次访问时创建连接并自动检测 MySQL 版本
    /// 
    /// ⚠️ 注意：DbContext 非线程安全，这里用单例简化
    /// 生产环境建议改用 DbContextFactory 或每次新建
    /// </summary>
    public static AppDbContext DbContext
    {
        get
        {
            if (_dbContext == null)
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString))
                    .Options;
                _dbContext = new AppDbContext(options);
            }
            return _dbContext;
        }
    }

    /// <summary>
    /// JSON 用户服务单例
    /// 文件路径：程序运行目录下的 Users.json
    /// </summary>
    public static JsonUserService UserService
    {
        get
        {
            if (_userService == null)
            {
                var path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\Users.json"));
                _userService = new JsonUserService(path);
            }
            return _userService;
        }
    }

    // ==================== Repository 快捷属性 ====================

    /// <summary>物品信息仓储</summary>
    public static ItemRepository ItemRepo => new(DbContext);

    /// <summary>入库记录仓储</summary>
    public static StockInRepository StockInRepo => new(DbContext);

    /// <summary>领用/出库记录仓储</summary>
    public static StockOutRepository StockOutRepo => new(DbContext);

    /// <summary>物品类别字典仓储</summary>
    public static CategoryRepository CategoryRepo => new(DbContext);

    /// <summary>产地字典仓储</summary>
    public static OriginRepository OriginRepo => new(DbContext);
}
