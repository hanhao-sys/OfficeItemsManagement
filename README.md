# 办公物品管理系统

## 项目概述

一个完整的小型管理信息系统，实现办公物品的入库、出库（领用审批）、库存查询及用户维护功能。系统分为**管理员端（WPF 桌面应用）**和**普通用户端（ASP.NET Core Razor Pages Web 应用）**。

## 📚 文档导航

| 文档 | 说明 |
|------|------|
| [部署指南](docs/部署指南.md) | 环境搭建、数据库初始化、编译运行 |
| [用户操作手册](docs/用户操作手册.md) | 管理员和普通用户的操作步骤 |
| [数据库设计文档](docs/数据库设计文档.md) | ER 图、表结构、存储过程、触发器、视图 |
| [系统架构文档](docs/系统架构文档.md) | 分层架构、设计模式、数据流 |
| [实验报告](docs/实验报告.md) | 实验目的、功能清单、加分项、总结 |

## 技术架构

| 层级 | 技术 | 说明 |
|------|------|------|
| 桌面端 | WPF (.NET 8) | 管理员功能 |
| Web 端 | ASP.NET Core Razor Pages (.NET 8) | 普通用户功能 |
| ORM | EF Core 8 + ADO.NET | 数据访问 |
| 数据库 | MySQL 8.4.8 LTS | 业务数据存储 |
| 用户存储 | JSON 文件 | 普通用户信息持久化 |

## 项目结构

```
OfficeItemsManagement/
├── OfficeItemsManagement.sln
├── OfficeItemsManagement.Common/         # 共享类库
│   ├── Models/          (Item, StockIn, StockOut, Category, Origin)
│   ├── DTOs/            (AppUserDto, StockQueryDto, etc.)
│   ├── Enums/           (ItemCategory, StockOutStatus)
│   └── Services/        (JsonUserService — JSON 文件用户CRUD)
├── OfficeItemsManagement.Data/           # 数据访问层
│   ├── AppDbContext.cs
│   └── Repositories/    (Item, StockIn, StockOut, Category, Origin)
├── OfficeItemsManagement.Admin/          # WPF 管理员端
│   ├── Views/
│   │   ├── LoginWindow          — 管理员登录
│   │   ├── MainWindow           — 主窗口（导航框架）
│   │   ├── UserManagementPage   — 用户维护（增删改）
│   │   ├── ItemManagementPage   — 物品信息维护（含图片上传）
│   │   ├── StockInPage          — 物品入库（自动流水号）
│   │   ├── StockOutPage         — 领用审批（确认/驳回）
│   │   └── StockQueryPage       — 库存查询（三个 Tab）
│   ├── ViewModels/   (ViewModelBase, RelayCommand)
│   └── Services/     (ServiceLocator)
├── OfficeItemsManagement.Web/            # ASP.NET Core Web 普通用户端
│   └── Pages/
│       ├── Account/Login       — 用户登录
│       ├── StockQuery          — 库存查询
│       └── Apply               — 领用申请（下拉+弹出窗口）
├── OfficeItemsManagement.Database/       # SQL 脚本
│   ├── 01_CreateDatabase.sql
│   ├── 02_CreateTables.sql      # 含完整性命名子句 (PK_, FK_, CK_, UQ_)
│   ├── 03_StoredProcedures.sql  # 9 个存储过程
│   ├── 04_Triggers.sql          # 4 个触发器
│   ├── 05_SeedData.sql          # 示例数据
│   ├── 06_SeedMillionRows.sql   # 100 万条测试数据生成
│   ├── 07_ViewsAndGrants.sql    # 视图 + GRANT/REVOKE 授权
│   └── 08_PerformanceTest.sql   # 性能测试与分析
└── docs/                                  # 文档
    ├── 部署指南.md
    ├── 用户操作手册.md
    ├── 数据库设计文档.md
    ├── 系统架构文档.md
    └── 实验报告.md
```

## 功能清单

### 管理员 (WPF)
- ✅ 登录（admin/0000，验证控件不能为空）
- ✅ **用户维护**：普通用户的增删改（JSON 文件存储）
- ✅ **物品信息维护**：增删改 + 类别单选 + 产地下拉 + 图片上传
- ✅ **物品入库**：自动生成流水号、自动计算总价、事务更新库存
- ✅ **物品出库**：查看领用申请、确认（扣库存）/ 驳回
- ✅ **库存查询**：库存总览 + 入库明细分页 + 领用明细分页

### 普通用户 (Web)
- ✅ 登录（验证 JSON 文件中的用户名密码）
- ✅ **库存查询**：查看物品库存信息
- ✅ **领用申请**：下拉选择 + 弹出窗口搜索选择物品、自动生成流水号

## 快速启动

### 1. 环境要求
- .NET 8 SDK
- MySQL 8.4.8 LTS
- Windows（WPF 仅支持 Windows）

### 2. 数据库初始化
```bash
# 按顺序执行 SQL 脚本
mysql -u root -p < OfficeItemsManagement.Database/01_CreateDatabase.sql
mysql -u root -p < OfficeItemsManagement.Database/02_CreateTables.sql
mysql -u root -p < OfficeItemsManagement.Database/03_StoredProcedures.sql
mysql -u root -p < OfficeItemsManagement.Database/04_Triggers.sql
mysql -u root -p < OfficeItemsManagement.Database/05_SeedData.sql
mysql -u root -p < OfficeItemsManagement.Database/07_ViewsAndGrants.sql
```

### 3. 配置连接字符串
修改 `OfficeItemsManagement.Admin/Services/ServiceLocator.cs` 和
`OfficeItemsManagement.Web/appsettings.json` 中的 MySQL 连接字符串。

### 4. 运行 Web 端
```bash
cd OfficeItemsManagement.Web
dotnet run
# 浏览器打开 http://localhost:5000
```

### 5. 运行 WPF 管理端
```bash
cd OfficeItemsManagement.Admin
dotnet run
# 或使用 Visual Studio 打开 .sln 运行
```

## 实验六加分项实现

| 要求 | 实现 |
|------|------|
| 🏆 100万条记录性能测试 | `06_SeedMillionRows.sql` + `08_PerformanceTest.sql`，含 EXPLAIN 分析和索引优化方案 |
| 🏆 高级数据库功能 | 存储过程（10个）、触发器（4个）、游标（1个） |
| 🏆 完整性命名子句 | PK_、FK_、CK_、UQ_ 前缀命名所有约束 |
| 🏆 视图 | 4 个视图：v_StockSummary、v_StockInMonthly、v_StockOutSummary、v_ItemFullInfo |
| 🏆 授权控制 | GRANT/REVOKE 示例，只读用户 + 应用用户权限分离 |
| 🏆 扩展SQL | 聚合查询、分页存储过程、多表 JOIN、CASE WHEN |

## 默认账号

| 角色 | 用户名 | 密码 | 登录入口 |
|------|--------|------|----------|
| 管理员 | admin | 0000 | WPF 桌面端 |
| 普通用户 | user01 | 1234 | Web 端 |
| 普通用户 | user02 | 1234 | Web 端 |
