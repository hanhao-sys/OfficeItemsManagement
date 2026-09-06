-- ============================================================
-- 办公物品管理系统 — 视图与授权控制（实验四/五集成）
-- ============================================================
USE OfficeItemsDB;

-- -----------------------------------------------------------
-- 视图1: 库存汇总视图（含类别名称）
-- -----------------------------------------------------------
CREATE VIEW v_StockSummary AS
SELECT
    i.ItemCode,
    i.ItemName,
    c.Name AS CategoryName,
    i.Origin,
    i.Specification,
    i.Model,
    i.Quantity,
    CASE
        WHEN i.Quantity = 0 THEN '缺货'
        WHEN i.Quantity < 50 THEN '低库存'
        WHEN i.Quantity < 200 THEN '正常'
        ELSE '充足'
    END AS StockLevel
FROM Items i
JOIN Categories c ON i.Category = c.Id;

-- -----------------------------------------------------------
-- 视图2: 入库汇总（按月统计）
-- -----------------------------------------------------------
CREATE VIEW v_StockInMonthly AS
SELECT
    DATE_FORMAT(PurchaseDate, '%Y-%m') AS Month,
    COUNT(*) AS RecordCount,
    SUM(Quantity) AS TotalQuantity,
    SUM(TotalPrice) AS TotalAmount
FROM StockIn
GROUP BY DATE_FORMAT(PurchaseDate, '%Y-%m')
ORDER BY Month DESC;

-- -----------------------------------------------------------
-- 视图3: 领用汇总（按物品统计）
-- -----------------------------------------------------------
CREATE VIEW v_StockOutSummary AS
SELECT
    so.ItemCode,
    i.ItemName,
    c.Name AS CategoryName,
    COUNT(*) AS ApplyCount,
    SUM(CASE WHEN so.Status = 0 THEN 1 ELSE 0 END) AS PendingCount,
    SUM(CASE WHEN so.Status = 1 THEN so.Quantity ELSE 0 END) AS ApprovedQty,
    SUM(CASE WHEN so.Status = 2 THEN 1 ELSE 0 END) AS RejectedCount
FROM StockOut so
JOIN Items i ON so.ItemCode = i.ItemCode
JOIN Categories c ON i.Category = c.Id
GROUP BY so.ItemCode, i.ItemName, c.Name;

-- -----------------------------------------------------------
-- 视图4: 物品完整信息（含入库总额、领用次数）
-- -----------------------------------------------------------
CREATE VIEW v_ItemFullInfo AS
SELECT
    i.ItemCode,
    i.ItemName,
    c.Name AS CategoryName,
    i.Origin,
    i.Specification,
    i.Model,
    i.Quantity,
    COALESCE(si.TotalInAmount, 0) AS TotalInAmount,
    COALESCE(so.TotalOutCount, 0) AS TotalOutCount
FROM Items i
JOIN Categories c ON i.Category = c.Id
LEFT JOIN (
    SELECT ItemCode, SUM(TotalPrice) AS TotalInAmount
    FROM StockIn GROUP BY ItemCode
) si ON i.ItemCode = si.ItemCode
LEFT JOIN (
    SELECT ItemCode, COUNT(*) AS TotalOutCount
    FROM StockOut WHERE Status = 1 GROUP BY ItemCode
) so ON i.ItemCode = so.ItemCode;

-- -----------------------------------------------------------
-- 授权控制（GRANT 示例 — 创建只读用户）
-- -----------------------------------------------------------
-- 创建只读角色用户
CREATE USER IF NOT EXISTS 'oi_readonly'@'localhost' IDENTIFIED BY 'readonly123';

-- 授予查询权限
GRANT SELECT ON OfficeItemsDB.v_StockSummary TO 'oi_readonly'@'localhost';
GRANT SELECT ON OfficeItemsDB.v_StockInMonthly TO 'oi_readonly'@'localhost';
GRANT SELECT ON OfficeItemsDB.v_StockOutSummary TO 'oi_readonly'@'localhost';
GRANT SELECT ON OfficeItemsDB.v_ItemFullInfo TO 'oi_readonly'@'localhost';

-- 创建应用用户
CREATE USER IF NOT EXISTS 'oi_app'@'localhost' IDENTIFIED BY 'app123';

-- 授予应用用户数据操作权限
GRANT SELECT, INSERT, UPDATE, DELETE ON OfficeItemsDB.Items TO 'oi_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON OfficeItemsDB.StockIn TO 'oi_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON OfficeItemsDB.StockOut TO 'oi_app'@'localhost';
GRANT SELECT ON OfficeItemsDB.Categories TO 'oi_app'@'localhost';
GRANT SELECT ON OfficeItemsDB.Origins TO 'oi_app'@'localhost';
GRANT EXECUTE ON PROCEDURE OfficeItemsDB.sp_StockIn TO 'oi_app'@'localhost';
GRANT EXECUTE ON PROCEDURE OfficeItemsDB.sp_ApproveStockOut TO 'oi_app'@'localhost';
GRANT EXECUTE ON PROCEDURE OfficeItemsDB.sp_RejectStockOut TO 'oi_app'@'localhost';

FLUSH PRIVILEGES;

-- 撤销权限示例（实验完成后）
-- REVOKE ALL PRIVILEGES ON OfficeItemsDB.* FROM 'oi_app'@'localhost';
-- DROP USER IF EXISTS 'oi_app'@'localhost';
-- DROP USER IF EXISTS 'oi_readonly'@'localhost';
