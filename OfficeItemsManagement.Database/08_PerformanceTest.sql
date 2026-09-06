-- ============================================================
-- 办公物品管理系统 — 100万条记录性能测试与分析
-- ============================================================
USE OfficeItemsDB;

-- -----------------------------------------------------------
-- 1. 插入前准备：确认当前记录数
-- -----------------------------------------------------------
SELECT COUNT(*) AS CurrentStockInCount FROM StockIn;

-- -----------------------------------------------------------
-- 2. 执行百万条数据插入
--    CALL sp_SeedMillionStockIn();
--    预计耗时: 5-15分钟（取决于硬件）
-- -----------------------------------------------------------

-- -----------------------------------------------------------
-- 3. 性能测试查询
-- -----------------------------------------------------------

-- 测试1: 全表扫描（无索引时约 2-5 秒）
SELECT COUNT(*) FROM StockIn;

-- 测试2: 按物品编码查询（有索引 — 预期 < 50ms）
SELECT * FROM StockIn WHERE ItemCode = 'P001' LIMIT 100;

-- 测试3: 按日期范围查询（有索引 — 预期 < 100ms）
SELECT * FROM StockIn
WHERE PurchaseDate BETWEEN '2025-01-01' AND '2025-06-30'
LIMIT 100;

-- 测试4: 聚合统计（无覆盖索引时较慢）
SELECT ItemCode, COUNT(*) AS Cnt, SUM(TotalPrice) AS Total
FROM StockIn
GROUP BY ItemCode;

-- 测试5: JOIN 查询（预期 100-500ms，取决于数据量）
SELECT si.StockInNo, i.ItemName, si.TotalPrice
FROM StockIn si
JOIN Items i ON si.ItemCode = i.ItemCode
WHERE si.PurchaseDate >= '2025-06-01'
LIMIT 50;

-- -----------------------------------------------------------
-- 4. 执行计划分析
-- -----------------------------------------------------------
EXPLAIN SELECT * FROM StockIn WHERE ItemCode = 'P001';
EXPLAIN SELECT ItemCode, SUM(TotalPrice) FROM StockIn GROUP BY ItemCode;

-- -----------------------------------------------------------
-- 5. 索引优化建议
-- -----------------------------------------------------------
-- 添加复合索引（如果查询经常同时按日期+物品）
-- CREATE INDEX IX_StockIn_Date_Item ON StockIn(PurchaseDate, ItemCode);

-- 覆盖索引（避免回表）
-- CREATE INDEX IX_StockIn_Cover ON StockIn(ItemCode, PurchaseDate, TotalPrice);

-- -----------------------------------------------------------
-- 6. 清理测试数据
-- -----------------------------------------------------------
-- DELETE FROM StockIn WHERE StockInNo LIKE 'SI2024%';
-- 注意: 触发器会同步更新 Items.Quantity，请谨慎操作
