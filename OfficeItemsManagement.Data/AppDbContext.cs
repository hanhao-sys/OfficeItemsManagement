using Microsoft.EntityFrameworkCore;
using OfficeItemsManagement.Common.Models;

namespace OfficeItemsManagement.Data;

/// <summary>
/// Entity Framework Core 数据库上下文
/// 映射 MySQL 数据库 OfficeItemsDB 的五张表
/// 
/// 使用 Pomelo.EntityFrameworkCore.MySql 提供程序连接 MySQL 8.4.8
/// 连接字符串在 ServiceLocator（管理端）和 appsettings.json（Web 端）中配置
/// </summary>
public class AppDbContext : DbContext
{
    // ==================== DbSet 属性 ====================

    /// <summary>物品信息表 Items</summary>
    public DbSet<Item> Items => Set<Item>();

    /// <summary>物品类别字典表 Categories</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>产地字典表 Origins</summary>
    public DbSet<Origin> Origins => Set<Origin>();

    /// <summary>入库记录表 StockIn</summary>
    public DbSet<StockIn> StockIns => Set<StockIn>();

    /// <summary>领用/出库记录表 StockOut</summary>
    public DbSet<StockOut> StockOuts => Set<StockOut>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Fluent API 配置 — 定义表名、主键、外键关系、默认值
    /// 与数据库 CREATE TABLE 中的完整性命名子句一一对应
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ======== Items 物品信息表 ========
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(e => e.ItemCode);              // PK_Items
            entity.Property(e => e.Quantity).HasDefaultValue(0); // CK_Items_Quantity >= 0
        });

        // ======== StockIn 入库记录表 ========
        modelBuilder.Entity<StockIn>(entity =>
        {
            entity.ToTable("StockIn");
            entity.HasKey(e => e.StockInNo);             // PK_StockIn
            entity.HasOne(e => e.Item)                   // FK_StockIn_ItemCode
                  .WithMany()
                  .HasForeignKey(e => e.ItemCode);
        });

        // ======== StockOut 领用/出库记录表 ========
        modelBuilder.Entity<StockOut>(entity =>
        {
            entity.ToTable("StockOut");
            entity.HasKey(e => e.StockOutNo);            // PK_StockOut
            entity.Property(e => e.Status).HasDefaultValue(0); // 默认"申请"状态
            entity.HasOne(e => e.Item)                   // FK_StockOut_ItemCode
                  .WithMany()
                  .HasForeignKey(e => e.ItemCode);
        });
    }
}
