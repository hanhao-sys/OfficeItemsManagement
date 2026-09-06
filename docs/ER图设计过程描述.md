# 办公物品管理系统 — ER 图设计过程描述

---

## 一、实体识别

通过对"办公物品管理系统"需求文档的分析，采用**自底向上**的方法，从数据流和数据存储中抽象出以下 5 个实体：

### 1.1 物品类别（Categories）

**识别依据**：系统要求物品信息中包含"物品类别"，类别有固定取值（纸张、文具、刀具、单据、礼品、其它），且物品页面用单选按钮呈现。这种固定选项集适合抽取为独立的字典实体，便于维护和扩展。

**属性**：
- Id — 类别编号，INT，自增主键
- Name — 类别名称，VARCHAR(50)，唯一约束

### 1.2 产地（Origins）

**识别依据**：需求明确"产地通过下拉式列表框实现"，下拉列表的数据源需要一个独立的字典表来存储。预置 13 个产地（北京、上海等国内城市 + 德国、日本、美国、中国台湾）。

**属性**：
- Id — 产地编号，INT，自增主键
- Name — 产地名称，VARCHAR(100)，唯一约束

### 1.3 物品（Items）

**识别依据**：这是系统的核心实体。需求要求管理员维护"物品编码、物品名称、物品类别、产地、规格、型号、物品图片"等信息，所有业务（入库、出库、库存查询）都围绕物品展开。

**属性**：
- ItemCode — 物品编码，VARCHAR(20)，主键
- ItemName — 物品名称，VARCHAR(100)，NOT NULL
- Category — 类别，INT，外键引用 Categories(Id)
- Origin — 产地，VARCHAR(100)
- Specification — 规格，VARCHAR(100)
- Model — 型号，VARCHAR(100)
- ImagePath — 图片文件路径，VARCHAR(500)
- ImageData — 图片二进制数据，LONGBLOB
- Quantity — 库存数量，INT，DEFAULT 0，CHECK(≥0)

### 1.4 入库记录（StockIn）

**识别依据**：需求要求"物品入库"功能，记录"入库流水号、物品编码、购买日期、购买数量、单价、总价"。入库操作的核心影响是增加物品库存，因此需要与 Items 实体建立关联。

**属性**：
- StockInNo — 入库流水号，VARCHAR(20)，主键，格式 SI+yyyyMMdd+序号
- ItemCode — 物品编码，VARCHAR(20)，外键引用 Items(ItemCode)
- PurchaseDate — 购买日期，DATETIME
- Quantity — 入库数量，INT，CHECK(>0)
- UnitPrice — 单价，DECIMAL(18,2)，CHECK(≥0)
- TotalPrice — 总价，DECIMAL(18,2)，CHECK(≥0)

### 1.5 领用出库记录（StockOut）

**识别依据**：需求要求"领用申请"和"领用审批"功能，涉及"领用流水号、物品编码、数量、领用人、领用日期、状态（申请/确认/驳回）"。状态字段是业务核心——通过它驱动审批流程和控制库存扣减。

**属性**：
- StockOutNo — 领用流水号，VARCHAR(20)，主键，格式 SO+yyyyMMdd+序号
- ItemCode — 物品编码，VARCHAR(20)，外键引用 Items(ItemCode)
- Quantity — 领用数量，INT，CHECK(>0)
- ApplicantId — 领用人 ID，VARCHAR(50)
- ApplyDate — 申请日期，DATETIME
- Status — 状态，INT，DEFAULT 0，CHECK(0/1/2)
- ApproveDate — 审批日期，DATETIME，NULL
- Remark — 审批备注，VARCHAR(500)，NULL

---

## 二、关系识别与描述

### 2.1 "属于"关系：Categories → Items

| 属性 | 说明 |
|------|------|
| 关系名 | 属于（Belongs\_To） |
| 参与实体 | Categories（父）、Items（子） |
| 映射基数 | **1:N** — 一个类别下可以有多个物品，一个物品只能属于一个类别 |
| 实现方式 | Items.Category 作为外键，引用 Categories.Id |
| 删除策略 | RESTRICT — 有物品属于某类别时，不允许删除该类别 |
| 业务含义 | 管理员在"物品信息"页面通过单选按钮选择类别，类别决定物品的分类归属 |

### 2.2 "参考"关系：Origins → Items

| 属性 | 说明 |
|------|------|
| 关系名 | 参考（Refers\_To） |
| 参与实体 | Origins（字典）、Items |
| 映射基数 | **1:N** — 一个产地可以被多个物品参考 |
| 实现方式 | Items.Origin 为 VARCHAR 字段，应用程序层面通过 ComboBox 下拉选择，数据库层面无强制外键约束（松散耦合） |
| 业务含义 | 管理员在"物品信息"页面通过下拉列表框选择产地 |

### 2.3 "入库"关系：Items → StockIn

| 属性 | 说明 |
|------|------|
| 关系名 | 入库（Stock\_In） |
| 参与实体 | Items、StockIn |
| 映射基数 | **1:N** — 一个物品可以有多条入库记录，每条入库记录只对应一个物品 |
| 实现方式 | StockIn.ItemCode 作为外键，引用 Items.ItemCode |
| 删除策略 | RESTRICT — 物品有入库记录时，不允许删除该物品（需先删除入库记录） |
| 级联影响 | 插入入库记录时，通过触发器 trg\_StockIn\_AfterInsert 自动累加 Items.Quantity；删除入库记录时，通过触发器自动回退库存 |
| 业务含义 | 管理员采购办公物品后，在"物品入库"页面登记：选择物品 → 输入数量和单价 → 系统自动生成流水号并累加库存 |

### 2.4 "领用"关系：Items → StockOut

| 属性 | 说明 |
|------|------|
| 关系名 | 领用（Stock\_Out） |
| 参与实体 | Items、StockOut |
| 映射基数 | **1:N** — 一个物品可以被多次领用，每条领用记录只对应一个物品 |
| 实现方式 | StockOut.ItemCode 作为外键，引用 Items.ItemCode |
| 删除策略 | RESTRICT — 物品有领用记录时，不允许删除该物品 |
| 级联影响 | 审批确认时（Status 0→1），通过触发器 trg\_StockOut\_AfterUpdate 自动扣减 Items.Quantity；删除已确认的领用记录时，自动恢复库存 |
| 业务含义 | 普通用户在 Web 端提交领用申请 → 管理员在桌面端确认或驳回 → 确认时自动扣减库存 |

---

## 三、ER 图设计要点总结

### 3.1 总关系描述

本系统数据库包含 **5 个实体** 和 **4 个关系**：

> Categories —(1:N)→ Items —(1:N)→ StockIn
>                                   
> Origins    —(松散引用)→ Items —(1:N)→ StockOut

### 3.2 设计决策说明

| 决策 | 理由 |
|------|------|
| 类别独立成表 | 方便未来扩展新类别，避免硬编码 |
| 产地独立成表 | 提供标准化的下拉列表数据源 |
| 产地不设外键 | 产地仅为参考信息，非强约束；允许管理员手动输入新产地 |
| 流水号用 VARCHAR | 需要 "SI+日期+序号" 的业务可读格式，自增 INT 无法满足 |
| Status 用 INT 而非枚举 | INT 便于程序比较和数据库 CHECK 约束，枚举类型的扩展性差 |
| ImageData 用 LONGBLOB | 比文件系统路径更可靠，备份数据库即备份图片 |
| 所有外键设 RESTRICT | 防止误删导致数据不一致，强制"先删子记录再删父记录" |

### 3.3 完整性约束汇总

| 约束类型 | 数量 | 示例 |
|----------|------|------|
| 主键（PK） | 5 | PK\_Items(ItemCode) |
| 外键（FK） | 3 | FK\_StockIn\_ItemCode → Items |
| CHECK 约束 | 6 | CK\_Items\_Quantity(>=0) |
| UNIQUE 约束 | 2 | UQ\_Categories\_Name |
| 索引（IX） | 7 | IX\_StockIn\_ItemCode |
